#!/usr/bin/env node
/**
 * PMDC MCP Server
 *
 * Provides tools to help Claude understand and work with the PMDC codebase:
 * - Documentation resources (architecture, flows, conventions)
 * - Class listing and documentation lookup
 * - Scaffolding for new BattleEvents, AI Plans, and GenSteps
 *
 * Uses tree-sitter for proper AST-based C# parsing.
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import * as fs from "fs";
import * as path from "path";
import { fileURLToPath } from "url";
import { Parser, Language, Node, Tree } from "web-tree-sitter";

// Get __dirname equivalent for ES modules
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Find the docs directory relative to the server
function findDocsDir(): string {
  const candidates = [
    path.resolve(__dirname, "../../docs/claude"),
    path.resolve(__dirname, "../docs/claude"),
    path.resolve(process.cwd(), "docs/claude"),
  ];

  for (const dir of candidates) {
    if (fs.existsSync(dir)) {
      return dir;
    }
  }

  return path.resolve(process.cwd(), "docs/claude");
}

// Find the PMDC source directory
function findPmdcDir(): string {
  const candidates = [
    path.resolve(__dirname, "../../PMDC"),
    path.resolve(__dirname, "../PMDC"),
    path.resolve(process.cwd(), "PMDC"),
  ];

  for (const dir of candidates) {
    if (fs.existsSync(dir)) {
      return dir;
    }
  }

  return path.resolve(process.cwd(), "PMDC");
}

const DOCS_DIR = findDocsDir();
const PMDC_DIR = findPmdcDir();

// =============================================================================
// TREE-SITTER INITIALIZATION
// =============================================================================

let csharpParser: Parser | null = null;
let csharpLanguage: Language | null = null;

async function initializeParser(): Promise<void> {
  if (csharpParser) return;

  await Parser.init();
  csharpParser = new Parser();

  // Load C# grammar from node_modules
  const wasmPath = path.resolve(__dirname, "../node_modules/tree-sitter-c-sharp/tree-sitter-c_sharp.wasm");
  csharpLanguage = await Language.load(wasmPath);
  csharpParser.setLanguage(csharpLanguage);
}

// =============================================================================
// TREE-SITTER HELPERS
// =============================================================================

/**
 * Find all nodes of a specific type in the tree.
 */
function findNodesByType(node: Node, type: string, results: Node[] = []): Node[] {
  if (node.type === type) results.push(node);
  for (let i = 0; i < node.childCount; i++) {
    const child = node.child(i);
    if (child) findNodesByType(child, type, results);
  }
  return results;
}

/**
 * Extract lines from source content around a given position.
 * Returns the line containing the position plus preceding context.
 */
function extractContextLines(
  content: string,
  startLine: number,
  endLine: number,
  contextLinesBefore: number = 1
): string {
  const lines = content.split('\n');
  const fromLine = Math.max(0, startLine - contextLinesBefore);
  const toLine = Math.min(lines.length - 1, endLine);
  return lines.slice(fromLine, toLine + 1).join('\n').trim();
}

/**
 * Extract instantiation examples from a C# file for known classes.
 */
async function extractExamplesFromFile(
  filePath: string,
  classNames: Set<string>
): Promise<Map<string, ClassExample[]>> {
  const examples = new Map<string, ClassExample[]>();

  try {
    await initializeParser();
    if (!csharpParser) return examples;

    let content = fs.readFileSync(filePath, "utf-8");
    if (content.charCodeAt(0) === 0xFEFF) {
      content = content.slice(1);
    }

    const tree = csharpParser.parse(content);
    if (!tree) return examples;

    // Find all object creation expressions: new ClassName(...)
    const creations = findNodesByType(tree.rootNode, "object_creation_expression");

    for (const creation of creations) {
      // Get the type being instantiated
      const typeNode = creation.childForFieldName("type");
      if (!typeNode) continue;

      // Extract base class name (handle generics like Foo<T>)
      let typeName = typeNode.text;
      const genericIndex = typeName.indexOf('<');
      if (genericIndex > 0) {
        typeName = typeName.substring(0, genericIndex);
      }

      // Check if this is a class we care about
      if (!classNames.has(typeName)) continue;

      // Extract context (the line + 1 before)
      const startLine = creation.startPosition.row;
      const endLine = creation.endPosition.row;
      const code = extractContextLines(content, startLine, endLine, 1);

      // Skip very long examples (likely complex nested structures)
      if (code.split('\n').length > 5) continue;

      const relativePath = filePath.replace(PMDC_DIR, "PMDC").replace(/\\/g, "/");

      const example: ClassExample = {
        code,
        file: relativePath,
        line: startLine + 1  // 1-indexed for display
      };

      if (!examples.has(typeName)) {
        examples.set(typeName, []);
      }
      examples.get(typeName)!.push(example);
    }
  } catch (err) {
    // Silently skip files that fail to parse
  }

  return examples;
}

/**
 * Collect all preceding doc comments (///) for a node.
 */
function getDocComments(node: Node): string {
  const comments: string[] = [];
  let prev = node.previousSibling;

  // Collect all consecutive comment siblings
  while (prev && prev.type === "comment") {
    comments.unshift(prev.text);
    prev = prev.previousSibling;
  }

  return comments.join("\n");
}

/**
 * Parse XML documentation from combined doc comment text.
 */
function parseDocComment(docText: string): { summary: string; remarks: string; inheritdoc: boolean } {
  // Extract summary
  const summaryMatch = docText.match(/<summary>\s*([\s\S]*?)\s*<\/summary>/);
  const summary = summaryMatch
    ? summaryMatch[1].replace(/^\s*\/\/\/\s*/gm, "").trim()
    : "";

  // Extract remarks
  const remarksMatch = docText.match(/<remarks>\s*([\s\S]*?)\s*<\/remarks>/);
  const remarks = remarksMatch
    ? remarksMatch[1].replace(/^\s*\/\/\/\s*/gm, "").trim()
    : "";

  // Check for inheritdoc
  const inheritdoc = docText.includes("<inheritdoc");

  return { summary, remarks, inheritdoc };
}

/**
 * Get the text of a field by name from a node.
 */
function getFieldText(node: Node, fieldName: string): string {
  const field = node.childForFieldName(fieldName);
  return field ? field.text : "";
}

// Class categories for organization
const CLASS_CATEGORIES = {
  battleevent: {
    dir: "Dungeon/GameEffects/BattleEvent",
    baseClass: "BattleEvent",
    description: "Battle effect handlers (damage, status, healing, etc.)"
  },
  aiplan: {
    dir: "Dungeon/AI",
    baseClass: "AIPlan",
    description: "AI behavior plans (attack, avoid, follow, etc.)"
  },
  genstep: {
    dir: "LevelGen/Floors/GenSteps",
    baseClass: "GenStep",
    description: "Floor generation steps"
  },
  zonestep: {
    dir: "LevelGen/Zones/ZoneSteps",
    baseClass: "ZoneStep",
    description: "Zone-level generation steps"
  },
  roomgen: {
    dir: "LevelGen/Floors/GenSteps/Rooms",
    baseClass: "RoomGen",
    description: "Room shape generators"
  },
  charstate: {
    dir: "Dungeon/GameEffects",
    baseClass: "CharState",
    description: "Character state markers"
  },
  statusstate: {
    dir: "Dungeon/GameEffects",
    baseClass: "StatusState",
    description: "Status effect data states"
  },
  contextstate: {
    dir: "Dungeon/GameEffects",
    baseClass: "ContextState",
    description: "Battle context states"
  },
  data: {
    dir: "Data",
    baseClass: "BaseData",
    description: "Game data models"
  }
} as const;

type ClassCategory = keyof typeof CLASS_CATEGORIES;

// Cache for extracted examples (lazy-loaded)
let examplesCache: Map<string, ClassExample[]> | null = null;
let knownClassNames: Set<string> | null = null;

// =============================================================================
// CLASS DOCUMENTATION PARSING (using tree-sitter)
// =============================================================================

interface ClassExample {
  code: string;       // 2-3 lines including the instantiation
  file: string;       // Relative path from PMDC/
  line: number;       // Starting line of snippet
}

interface ClassDoc {
  name: string;
  namespace: string;
  baseClass: string;
  summary: string;
  remarks: string;
  properties: Array<{ name: string; type: string; summary: string }>;
  methods: Array<{ name: string; signature: string; summary: string }>;
  filePath: string;
  examples: ClassExample[];
}

/**
 * Parse all classes from a C# file using tree-sitter AST.
 */
async function parseClassFile(filePath: string): Promise<ClassDoc[]> {
  try {
    await initializeParser();
    if (!csharpParser) return [];

    let content = fs.readFileSync(filePath, "utf-8");

    // Strip UTF-8 BOM if present
    if (content.charCodeAt(0) === 0xFEFF) {
      content = content.slice(1);
    }

    const tree = csharpParser.parse(content);
    if (!tree) return [];

    const results: ClassDoc[] = [];

    // Extract namespace
    const namespaceDecls = findNodesByType(tree.rootNode, "namespace_declaration");
    const namespace = namespaceDecls.length > 0 ? getFieldText(namespaceDecls[0], "name") : "";

    // Find all class declarations
    const classDecls = findNodesByType(tree.rootNode, "class_declaration");

    for (const classNode of classDecls) {
      const className = getFieldText(classNode, "name");
      if (!className) continue;

      // Get base class from base_list
      const baseLists = findNodesByType(classNode, "base_list");
      let baseClass = "";
      if (baseLists.length > 0) {
        // Extract first base type (skip the colon)
        const baseList = baseLists[0];
        for (let i = 0; i < baseList.childCount; i++) {
          const child = baseList.child(i);
          if (child && child.type !== ":") {
            baseClass = child.text.split(",")[0].trim();
            break;
          }
        }
      }

      // Get doc comments for the class
      const classDoc = getDocComments(classNode);
      const { summary, remarks } = parseDocComment(classDoc);

      // Extract fields (PMDC uses public fields, not properties)
      const properties: ClassDoc["properties"] = [];
      const fieldDecls = findNodesByType(classNode, "field_declaration");

      for (const field of fieldDecls) {
        const fieldDoc = getDocComments(field);
        const { summary: fieldSummary } = parseDocComment(fieldDoc);

        // Get type from variable_declaration child
        // Structure: field_declaration -> variable_declaration -> type
        const varDecl = findNodesByType(field, "variable_declaration")[0];
        let fieldType = "unknown";
        if (varDecl) {
          // Try to get type as a named field first
          const typeNode = varDecl.childForFieldName("type");
          if (typeNode) {
            fieldType = typeNode.text;
          } else {
            // Fallback: find first child that looks like a type
            for (let i = 0; i < varDecl.childCount; i++) {
              const child = varDecl.child(i);
              if (child && child.type !== "variable_declarator" && child.type !== "," && child.type !== ";") {
                fieldType = child.text;
                break;
              }
            }
          }
        }

        // Get variable name(s)
        const variableDeclarators = findNodesByType(field, "variable_declarator");
        for (const declarator of variableDeclarators) {
          const varName = getFieldText(declarator, "name") || declarator.text.split("=")[0].trim();
          properties.push({
            name: varName,
            type: fieldType,
            summary: fieldSummary || ""
          });
        }
      }

      // Extract methods
      const methods: ClassDoc["methods"] = [];
      const methodDecls = findNodesByType(classNode, "method_declaration");

      for (const method of methodDecls) {
        const methodDoc = getDocComments(method);
        const { summary: methodSummary, inheritdoc } = parseDocComment(methodDoc);

        const methodName = getFieldText(method, "name");
        const paramsNode = method.childForFieldName("parameters");
        const params = paramsNode ? paramsNode.text : "()";

        // Get return type - try multiple approaches
        let returnType = "void";
        // Try "type" field first (standard location)
        const typeNode = method.childForFieldName("type");
        if (typeNode) {
          returnType = typeNode.text;
        } else {
          // Fallback: look for type before the method name
          // Method structure: modifiers type name parameters body
          for (let i = 0; i < method.childCount; i++) {
            const child = method.child(i);
            if (!child) continue;
            // Stop when we hit the method name
            if (child.type === "identifier" && child.text === methodName) break;
            // Skip modifiers and attributes
            if (child.type === "modifier" || child.type === "attribute_list") continue;
            // This should be the return type
            if (child.type.includes("type") ||
                child.type === "predefined_type" ||
                child.type === "identifier" ||
                child.type === "generic_name" ||
                child.type === "qualified_name" ||
                child.type === "nullable_type" ||
                child.type === "array_type") {
              returnType = child.text;
              break;
            }
          }
        }

        const signature = `${returnType} ${methodName}${params}`;

        methods.push({
          name: methodName,
          signature,
          summary: inheritdoc ? "(inherited documentation)" : (methodSummary || "")
        });
      }

      results.push({
        name: className,
        namespace,
        baseClass,
        summary,
        remarks,
        properties,
        methods,
        filePath,
        examples: []
      });
    }

    return results;
  } catch (err) {
    console.error(`Error parsing ${filePath}:`, err);
    return [];
  }
}

/**
 * Check if a class inherits from a target base class (directly or indirectly through naming).
 */
function matchesBaseClass(classBaseClass: string, targetBaseClass: string, className?: string): boolean {
  // Also match if this IS the base class itself (e.g., AIPlan class for aiplan category)
  if (className && className === targetBaseClass) return true;

  if (!classBaseClass) return false;

  // Direct match
  if (classBaseClass === targetBaseClass) return true;

  // Handle generic base classes like "GenStep<T>"
  const baseWithoutGenerics = classBaseClass.replace(/<[^>]+>/, "");
  if (baseWithoutGenerics === targetBaseClass) return true;

  // Handle inheritance chains by checking if the base class NAME contains the target
  // e.g., "AttackFoesPlan" has base "AIPlan" -> matches
  // e.g., "ChestStep" has base "MonsterHouseBaseStep<T>" -> contains "Step"
  if (classBaseClass.includes(targetBaseClass)) return true;

  return false;
}

async function findClassesInCategory(category: ClassCategory): Promise<ClassDoc[]> {
  const categoryInfo = CLASS_CATEGORIES[category];
  const categoryDir = path.join(PMDC_DIR, categoryInfo.dir);

  if (!fs.existsSync(categoryDir)) {
    return [];
  }

  const classes: ClassDoc[] = [];
  const filesToParse: string[] = [];

  function collectFiles(dir: string, skipSubdirs: string[] = []) {
    const entries = fs.readdirSync(dir, { withFileTypes: true });

    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);

      if (entry.isDirectory() && !entry.name.startsWith(".") && entry.name !== "obj" && entry.name !== "bin") {
        // Skip specified subdirectories to avoid overlap
        if (!skipSubdirs.includes(entry.name)) {
          collectFiles(fullPath);
        }
      } else if (entry.isFile() && entry.name.endsWith(".cs")) {
        filesToParse.push(fullPath);
      }
    }
  }

  // For categories in the shared GameEffects directory, skip subdirectories
  // that belong to other categories to prevent overlap
  if (category === "charstate" || category === "statusstate" || category === "contextstate") {
    collectFiles(categoryDir, ["BattleEvent"]);
  } else {
    collectFiles(categoryDir);
  }

  // Parse all files
  for (const filePath of filesToParse) {
    const fileDocs = await parseClassFile(filePath);
    for (const classDoc of fileDocs) {
      // Filter by base class to ensure we only get classes of the right type
      // Also pass the class name to match the base class itself (e.g., AIPlan for aiplan category)
      if (matchesBaseClass(classDoc.baseClass, categoryInfo.baseClass, classDoc.name)) {
        classes.push(classDoc);
      }
    }
  }

  return classes;
}

async function findClassByName(className: string): Promise<ClassDoc | null> {
  for (const category of Object.keys(CLASS_CATEGORIES) as ClassCategory[]) {
    const classes = await findClassesInCategory(category);
    const found = classes.find(c => c.name.toLowerCase() === className.toLowerCase());
    if (found) return found;
  }
  return null;
}

/**
 * Simple Levenshtein distance for fuzzy matching.
 */
function levenshteinDistance(a: string, b: string): number {
  const matrix: number[][] = [];

  for (let i = 0; i <= b.length; i++) {
    matrix[i] = [i];
  }
  for (let j = 0; j <= a.length; j++) {
    matrix[0][j] = j;
  }

  for (let i = 1; i <= b.length; i++) {
    for (let j = 1; j <= a.length; j++) {
      if (b.charAt(i - 1) === a.charAt(j - 1)) {
        matrix[i][j] = matrix[i - 1][j - 1];
      } else {
        matrix[i][j] = Math.min(
          matrix[i - 1][j - 1] + 1,
          matrix[i][j - 1] + 1,
          matrix[i - 1][j] + 1
        );
      }
    }
  }

  return matrix[b.length][a.length];
}

/**
 * Find similar class names for suggestions.
 */
async function findSimilarClasses(searchName: string, limit: number = 5): Promise<Array<{ name: string; category: string; score: number }>> {
  const searchLower = searchName.toLowerCase();
  const allMatches: Array<{ name: string; category: string; score: number }> = [];

  for (const category of Object.keys(CLASS_CATEGORIES) as ClassCategory[]) {
    const classes = await findClassesInCategory(category);

    for (const cls of classes) {
      const nameLower = cls.name.toLowerCase();

      // Calculate similarity score (lower is better)
      let score = levenshteinDistance(searchLower, nameLower);

      // Boost score if the search term is a substring
      if (nameLower.includes(searchLower) || searchLower.includes(nameLower)) {
        score = Math.max(0, score - 5);
      }

      // Boost if key parts match (e.g., "Damage" in "DamageFormulaEvent" vs "ChipDamageEvent")
      const searchParts = searchLower.replace(/event|plan|step|state/gi, "").trim();
      if (searchParts && nameLower.includes(searchParts)) {
        score = Math.max(0, score - 3);
      }

      allMatches.push({ name: cls.name, category, score });
    }
  }

  // Sort by score (lower is better) and return top matches
  return allMatches
    .sort((a, b) => a.score - b.score)
    .slice(0, limit);
}

// Create MCP server
const server = new McpServer({
  name: "pmdc-mcp-server",
  version: "1.0.0"
});

// =============================================================================
// RESOURCES - Documentation files
// =============================================================================

const DOC_FILES = ["architecture", "flows", "conventions"] as const;

for (const docName of DOC_FILES) {
  server.resource(
    `pmdc-docs-${docName}`,
    `pmdc://docs/${docName}`,
    async () => {
      const filePath = path.join(DOCS_DIR, `${docName}.md`);
      try {
        const content = fs.readFileSync(filePath, "utf-8");
        return {
          contents: [{
            uri: `pmdc://docs/${docName}`,
            mimeType: "text/markdown",
            text: content
          }]
        };
      } catch {
        return {
          contents: [{
            uri: `pmdc://docs/${docName}`,
            mimeType: "text/plain",
            text: `Error: Could not read ${docName}.md from ${DOCS_DIR}`
          }]
        };
      }
    }
  );
}

// =============================================================================
// RESOURCES - Class categories (browsable)
// =============================================================================

for (const [category, info] of Object.entries(CLASS_CATEGORIES)) {
  server.resource(
    `pmdc-classes-${category}`,
    `pmdc://classes/${category}`,
    async () => {
      const classes = await findClassesInCategory(category as ClassCategory);

      const lines = [
        `# ${category} Classes`,
        "",
        `**Description:** ${info.description}`,
        `**Base Class:** \`${info.baseClass}\``,
        `**Directory:** \`PMDC/${info.dir}\``,
        `**Count:** ${classes.length}`,
        "",
        "## Classes",
        ""
      ];

      for (const cls of classes) {
        lines.push(`### ${cls.name}`);
        if (cls.summary) {
          lines.push(cls.summary);
        }
        lines.push(`- **Base:** \`${cls.baseClass}\``);
        if (cls.properties.length > 0) {
          const propNames = cls.properties.slice(0, 5).map(p => p.name).join(", ");
          const more = cls.properties.length > 5 ? `, ... (+${cls.properties.length - 5} more)` : "";
          lines.push(`- **Properties:** ${propNames}${more}`);
        }
        lines.push("");
      }

      return {
        contents: [{
          uri: `pmdc://classes/${category}`,
          mimeType: "text/markdown",
          text: lines.join("\n")
        }]
      };
    }
  );
}

// =============================================================================
// PROMPTS
// =============================================================================

server.prompt(
  "create_battleevent",
  "Step-by-step guide for creating a new BattleEvent",
  () => ({
    messages: [{
      role: "user",
      content: {
        type: "text",
        text: `I need to create a new BattleEvent for PMDC. Please help me through the process:

1. First, use pmdc_list_classes with category "battleevent" to show me existing examples
2. Ask me what effect I want to create
3. Use pmdc_scaffold_battleevent to generate the boilerplate
4. Explain what I need to fill in based on similar existing events

Remember:
- BattleEvents use IEnumerator<YieldInstruction> for async execution
- Always implement Clone() with the copy constructor pattern
- Use context.ContextStates for battle calculation state
- Use DungeonScene.Instance.LogMsg() for in-game messages`
      }
    }]
  })
);

server.prompt(
  "create_aiplan",
  "Step-by-step guide for creating a new AIPlan",
  () => ({
    messages: [{
      role: "user",
      content: {
        type: "text",
        text: `I need to create a new AIPlan for PMDC. Please help me through the process:

1. First, use pmdc_list_classes with category "aiplan" to show me existing examples
2. Ask me what behavior I want to create
3. Use pmdc_scaffold_aiplan to generate the boilerplate
4. Explain the Think() method pattern based on similar existing plans

Remember:
- Think() returns GameAction or null (to fall through to next plan)
- Use GetAcceptableTargets() for target selection
- Use GetPathsBlocked() for A* pathfinding
- Mark runtime state with [NonSerialized]`
      }
    }]
  })
);

server.prompt(
  "create_genstep",
  "Step-by-step guide for creating a new GenStep",
  () => ({
    messages: [{
      role: "user",
      content: {
        type: "text",
        text: `I need to create a new GenStep for PMDC. Please help me through the process:

1. First, use pmdc_list_classes with category "genstep" to show me existing examples
2. Ask me what generation behavior I want to create
3. Help me choose the right context type:
   - ITiledGenContext: Basic tile operations
   - IFloorPlanGenContext: Room-based planning
   - BaseMapGenContext: Full map features
4. Use pmdc_scaffold_genstep to generate the boilerplate
5. Explain the Apply() pattern based on similar existing steps

Remember:
- GenSteps are queued with Priority levels
- Access map.RoomPlan for room-based operations
- Access map.Map for direct tile/entity manipulation`
      }
    }]
  })
);

// =============================================================================
// TOOLS
// =============================================================================

// Tool: Search across all categories
server.tool(
  "pmdc_search",
  `Search for PMDC classes across all categories by name or summary.

Searches class names and XML documentation summaries. Returns matches ranked by relevance.
Use this when you're not sure which category a class belongs to, or to find classes related to a concept.`,
  {
    query: z.string()
      .min(2)
      .describe("Search query (e.g., 'damage', 'poison', 'spawn')"),
    limit: z.number()
      .min(1)
      .max(50)
      .default(10)
      .describe("Maximum results to return"),
    response_format: z.enum(["markdown", "json"])
      .default("markdown")
      .describe("Output format")
  },
  async ({ query, limit, response_format }) => {
    const queryLower = query.toLowerCase();
    const results: Array<{
      name: string;
      category: ClassCategory;
      summary: string;
      score: number;
    }> = [];

    // Split query into words for multi-word matching
    const queryWords = queryLower.split(/\s+/).filter(w => w.length >= 2);

    // Search across all categories
    for (const category of Object.keys(CLASS_CATEGORIES) as ClassCategory[]) {
      const classes = await findClassesInCategory(category);

      for (const cls of classes) {
        const nameLower = cls.name.toLowerCase();
        const summaryLower = (cls.summary || "").toLowerCase();

        // Also search in property and method names
        const propNames = cls.properties.map(p => p.name.toLowerCase()).join(" ");
        const methodNames = cls.methods.map(m => m.name.toLowerCase()).join(" ");
        const allText = `${nameLower} ${summaryLower} ${propNames} ${methodNames}`;

        let score = 1000; // Start with high score (lower is better)

        // Exact name match
        if (nameLower === queryLower) {
          score = 0;
        }
        // Name starts with query
        else if (nameLower.startsWith(queryLower)) {
          score = 10;
        }
        // Name contains query
        else if (nameLower.includes(queryLower)) {
          score = 20;
        }
        // All query words found in name (for multi-word queries like "sleep event")
        else if (queryWords.length > 1 && queryWords.every(w => nameLower.includes(w))) {
          score = 25;
        }
        // Summary contains query
        else if (summaryLower.includes(queryLower)) {
          score = 50;
        }
        // All query words found in summary
        else if (queryWords.length > 1 && queryWords.every(w => summaryLower.includes(w))) {
          score = 55;
        }
        // Property or method name contains query
        else if (propNames.includes(queryLower) || methodNames.includes(queryLower)) {
          score = 70;
        }
        // Any query word found in any text
        else if (queryWords.some(w => allText.includes(w))) {
          score = 80;
        }
        // Fuzzy match on name
        else {
          const distance = levenshteinDistance(queryLower, nameLower);
          if (distance <= 3) {
            score = 100 + distance;
          } else {
            continue; // Skip if no match
          }
        }

        results.push({
          name: cls.name,
          category,
          summary: cls.summary || "(no documentation)",
          score
        });
      }
    }

    // Sort by score and limit
    const sorted = results.sort((a, b) => a.score - b.score).slice(0, limit);

    if (sorted.length === 0) {
      return {
        content: [{
          type: "text",
          text: `No classes found matching '${query}'. Try a different search term or use pmdc_list_classes to browse by category.`
        }]
      };
    }

    if (response_format === "json") {
      return {
        content: [{
          type: "text",
          text: JSON.stringify({ query, count: sorted.length, results: sorted }, null, 2)
        }]
      };
    }

    // Markdown format
    const lines = [
      `# Search Results: "${query}"`,
      "",
      `Found ${sorted.length} matching classes:`,
      "",
      "| Class | Category | Summary |",
      "|-------|----------|---------|"
    ];

    for (const result of sorted) {
      const summary = result.summary.length > 60
        ? result.summary.substring(0, 57) + "..."
        : result.summary;
      lines.push(`| \`${result.name}\` | ${result.category} | ${summary} |`);
    }

    lines.push("");
    lines.push("*Use `pmdc_get_class_docs` for full documentation on any class.*");

    return {
      content: [{ type: "text", text: lines.join("\n") }]
    };
  }
);

// Tool: List classes in a category
server.tool(
  "pmdc_list_classes",
  `List all PMDC classes in a specific category.

Categories: battleevent, aiplan, genstep, zonestep, roomgen, charstate, statusstate, contextstate, data

Returns class names with brief summaries from XML documentation.`,
  {
    category: z.enum([
      "battleevent", "aiplan", "genstep", "zonestep",
      "roomgen", "charstate", "statusstate", "contextstate", "data"
    ]).describe("Category of classes to list"),
    response_format: z.enum(["markdown", "json"])
      .default("markdown")
      .describe("Output format")
  },
  async ({ category, response_format }) => {
    const classes = await findClassesInCategory(category);
    const categoryInfo = CLASS_CATEGORIES[category];

    if (classes.length === 0) {
      return {
        content: [{
          type: "text",
          text: `No classes found in category '${category}' (searched ${categoryInfo.dir})`
        }]
      };
    }

    const output = {
      category,
      description: categoryInfo.description,
      baseClass: categoryInfo.baseClass,
      count: classes.length,
      classes: classes.map(c => ({
        name: c.name,
        summary: c.summary || "(no documentation)",
        baseClass: c.baseClass
      }))
    };

    if (response_format === "json") {
      return {
        content: [{ type: "text", text: JSON.stringify(output, null, 2) }]
      };
    }

    // Markdown format
    const lines = [
      `# ${category} Classes`,
      "",
      `**Description:** ${categoryInfo.description}`,
      `**Base Class:** \`${categoryInfo.baseClass}\``,
      `**Count:** ${classes.length}`,
      "",
      "| Class | Summary |",
      "|-------|---------|"
    ];

    for (const cls of classes) {
      const summary = cls.summary ? cls.summary.substring(0, 80) : "(no docs)";
      lines.push(`| \`${cls.name}\` | ${summary} |`);
    }

    return {
      content: [{ type: "text", text: lines.join("\n") }]
    };
  }
);

// Tool: Get class documentation
server.tool(
  "pmdc_get_class_docs",
  `Get detailed XML documentation for a specific PMDC class.

Extracts from C# source files:
- Class summary and remarks
- Namespace and base class
- Public properties with their types and documentation
- Public methods with their signatures and documentation`,
  {
    class_name: z.string()
      .min(1)
      .describe("Name of the class to get documentation for"),
    response_format: z.enum(["markdown", "json"])
      .default("markdown")
      .describe("Output format")
  },
  async ({ class_name, response_format }) => {
    const classDoc = await findClassByName(class_name);

    if (!classDoc) {
      // Find similar classes for suggestions
      const suggestions = await findSimilarClasses(class_name, 5);

      let errorMsg = `Class '${class_name}' not found in PMDC.\n\n`;

      if (suggestions.length > 0) {
        errorMsg += "**Did you mean one of these?**\n\n";
        for (const suggestion of suggestions) {
          errorMsg += `- \`${suggestion.name}\` (${suggestion.category})\n`;
        }
        errorMsg += "\n";
      }

      errorMsg += "*Note: This MCP only indexes PMDC classes. For RogueEssence engine classes, check the RogueEssence submodule directly.*";

      return {
        content: [{
          type: "text",
          text: errorMsg
        }]
      };
    }

    if (response_format === "json") {
      return {
        content: [{ type: "text", text: JSON.stringify(classDoc, null, 2) }]
      };
    }

    // Markdown format
    const lines = [
      `# ${classDoc.name}`,
      "",
      `**Namespace:** \`${classDoc.namespace}\``,
      `**Base Class:** \`${classDoc.baseClass}\``,
      `**File:** \`${classDoc.filePath.replace(PMDC_DIR, "PMDC")}\``,
      ""
    ];

    if (classDoc.summary) {
      lines.push("## Summary", "", classDoc.summary, "");
    }

    if (classDoc.remarks) {
      lines.push("## Remarks", "", classDoc.remarks, "");
    }

    if (classDoc.properties.length > 0) {
      lines.push("## Properties", "");
      for (const prop of classDoc.properties) {
        lines.push(`### \`${prop.name}\` : \`${prop.type}\``);
        if (prop.summary) lines.push(prop.summary);
        lines.push("");
      }
    }

    if (classDoc.methods.length > 0) {
      lines.push("## Methods", "");
      for (const method of classDoc.methods) {
        lines.push(`### \`${method.signature}\``);
        if (method.summary) lines.push(method.summary);
        lines.push("");
      }
    }

    return {
      content: [{ type: "text", text: lines.join("\n") }]
    };
  }
);

// Tool: Scaffold BattleEvent
server.tool(
  "pmdc_scaffold_battleevent",
  `Generate boilerplate C# code for a new BattleEvent.

Creates a properly structured class with:
- [Serializable] attribute
- Parameterless constructor
- Copy constructor
- Clone() method override
- Apply() coroutine method stub`,
  {
    name: z.string()
      .min(1)
      .describe("Name for the BattleEvent class"),
    description: z.string()
      .describe("What this battle event does")
  },
  async ({ name, description }) => {
    const className = name.endsWith("BattleEvent") ? name : `${name}BattleEvent`;

    const code = `using System;
using System.Collections.Generic;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using RogueElements;

namespace PMDC.Dungeon
{
    /// <summary>
    /// ${description}
    /// </summary>
    [Serializable]
    public class ${className} : BattleEvent
    {
        // Add your configuration properties here
        // public int SomeValue;
        // public string StatusID;

        public ${className}() { }

        protected ${className}(${className} other)
        {
            // Copy all fields from other
        }

        public override GameEvent Clone() { return new ${className}(this); }

        public override IEnumerator<YieldInstruction> Apply(
            GameEventOwner owner,
            Character ownerChar,
            BattleContext context)
        {
            // TODO: Implement your effect logic here
            yield break;
        }
    }
}`;

    return {
      content: [{ type: "text", text: code }]
    };
  }
);

// Tool: Scaffold AIPlan
server.tool(
  "pmdc_scaffold_aiplan",
  `Generate boilerplate C# code for a new AIPlan.

Creates a properly structured class with:
- [Serializable] attribute
- Constructor with base AIPlan parameters
- Copy constructor
- CreateNew() override
- Think() method stub`,
  {
    name: z.string()
      .min(1)
      .describe("Name for the AIPlan class"),
    description: z.string()
      .describe("What this AI plan does")
  },
  async ({ name, description }) => {
    const className = name.endsWith("Plan") ? name : `${name}Plan`;

    const code = `using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence;
using RogueEssence.Data;
using RogueEssence.Dungeon;

namespace PMDC.Dungeon
{
    /// <summary>
    /// ${description}
    /// </summary>
    [Serializable]
    public class ${className} : AIPlan
    {
        [NonSerialized]
        private List<Loc> goalPath;

        public ${className}() { }

        public ${className}(AIFlags iq, int attackRange, int statusRange, int selfStatusRange,
            TerrainData.Mobility restrictedMobilityTypes, bool restrictMobilityPassable)
            : base(iq, attackRange, statusRange, selfStatusRange,
                   restrictedMobilityTypes, restrictMobilityPassable)
        {
        }

        protected ${className}(${className} other) : base(other) { }

        public override BasePlan CreateNew() { return new ${className}(this); }

        public override void Initialize(Character controlledChar)
        {
            base.Initialize(controlledChar);
            goalPath = null;
        }

        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            // TODO: Implement your AI logic here
            // Return GameAction or null to fall through
            return null;
        }
    }
}`;

    return {
      content: [{ type: "text", text: code }]
    };
  }
);

// Tool: Scaffold GenStep
server.tool(
  "pmdc_scaffold_genstep",
  `Generate boilerplate C# code for a new GenStep.

Creates a properly structured class with:
- [Serializable] attribute
- Generic type constraint
- Apply() method stub`,
  {
    name: z.string()
      .min(1)
      .describe("Name for the GenStep class"),
    description: z.string()
      .describe("What this generation step does"),
    context_type: z.enum(["ITiledGenContext", "IFloorPlanGenContext", "BaseMapGenContext"])
      .default("BaseMapGenContext")
      .describe("The context interface this step requires")
  },
  async ({ name, description, context_type }) => {
    const className = name.endsWith("Step") ? name : `${name}Step`;

    const code = `using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.LevelGen;
using RogueEssence.Dungeon;

namespace PMDC.LevelGen
{
    /// <summary>
    /// ${description}
    /// </summary>
    [Serializable]
    public class ${className}<T> : GenStep<T> where T : class, ${context_type}
    {
        public ${className}() { }

        public override void Apply(T map)
        {
            // TODO: Implement your generation logic here
        }
    }
}`;

    return {
      content: [{ type: "text", text: code }]
    };
  }
);

// Tool: Scaffold RoomGen
server.tool(
  "pmdc_scaffold_roomgen",
  `Generate boilerplate C# code for a new RoomGen.

Creates a properly structured class with:
- [Serializable] attribute
- Generic type constraint
- Copy constructor
- Copy() override
- DrawOnMap() method stub
- PrepareFulfillableBorders() override for room connectivity`,
  {
    name: z.string()
      .min(1)
      .describe("Name for the RoomGen class (e.g., 'LShape', 'Diamond')"),
    description: z.string()
      .describe("What shape this room generator creates")
  },
  async ({ name, description }) => {
    const className = name.startsWith("RoomGen") ? name : `RoomGen${name}`;

    const code = `using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.LevelGen;

namespace PMDC.LevelGen
{
    /// <summary>
    /// ${description}
    /// </summary>
    [Serializable]
    public class ${className}<T> : RoomGen<T> where T : ITiledGenContext
    {
        /// <summary>
        /// Width of the room.
        /// </summary>
        public RandRange Width;

        /// <summary>
        /// Height of the room.
        /// </summary>
        public RandRange Height;

        public ${className}() { }

        public ${className}(RandRange width, RandRange height)
        {
            Width = width;
            Height = height;
        }

        protected ${className}(${className}<T> other)
        {
            Width = other.Width;
            Height = other.Height;
        }

        public override RoomGen<T> Copy() { return new ${className}<T>(this); }

        public override Loc ProposeSize(IRandom rand)
        {
            return new Loc(Width.Pick(rand), Height.Pick(rand));
        }

        public override void DrawOnMap(T map)
        {
            // Get the room bounds from the floor plan
            Rect roomRect = new Rect(Draw.Start, Draw.Size);

            // TODO: Draw your room shape here
            // Example: Fill with floor tiles
            for (int x = roomRect.X; x < roomRect.End.X; x++)
            {
                for (int y = roomRect.Y; y < roomRect.End.Y; y++)
                {
                    map.SetTile(new Loc(x, y), map.RoomTerrain.Copy());
                }
            }

            // Required: Set room borders for connectivity
            SetRoomBorders(map);
        }

        protected override void PrepareFulfillableBorders(IRandom rand)
        {
            // Define which borders can accept hallway connections
            // Call FulfillableBorder.Add for each valid connection point

            // Example: Allow connections on all four sides at the center
            int centerX = Draw.Width / 2;
            int centerY = Draw.Height / 2;

            // Top border
            FulfillableBorder[Dir4.Up].Add(new IntRange(centerX, centerX + 1));
            // Bottom border
            FulfillableBorder[Dir4.Down].Add(new IntRange(centerX, centerX + 1));
            // Left border
            FulfillableBorder[Dir4.Left].Add(new IntRange(centerY, centerY + 1));
            // Right border
            FulfillableBorder[Dir4.Right].Add(new IntRange(centerY, centerY + 1));
        }
    }
}`;

    return {
      content: [{ type: "text", text: code }]
    };
  }
);

// =============================================================================
// SERVER STARTUP
// =============================================================================

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("PMDC MCP server running via stdio");
}

main().catch(error => {
  console.error("Server error:", error);
  process.exit(1);
});
