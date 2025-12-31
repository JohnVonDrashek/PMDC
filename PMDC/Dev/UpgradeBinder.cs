using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;

namespace PMDC.Dev
{
    /// <summary>
    /// Custom serialization binder that handles type mapping for backward compatibility
    /// during deserialization of older save files. Maps renamed or moved types to their
    /// current implementations.
    /// </summary>
    public sealed class UpgradeBinder : DefaultSerializationBinder
    {
        /// <inheritdoc/>
        /// <remarks>
        /// Applies the following version-specific type mappings for backward compatibility:
        /// <list type="bullet">
        /// <item><description>v0.7.0: Maps RogueEssence.LevelGen.FloorNameIDZoneStep to PMDC.LevelGen.FloorNameDropZoneStep</description></item>
        /// <item><description>v0.7.21: Maps PMDC.Dungeon.AllyDifferentEvent to PMDC.Dungeon.AlignmentDifferentEvent</description></item>
        /// <item><description>Legacy: Maps RefreshPreEvent to ElementMobilityEvent</description></item>
        /// <item><description>Legacy: Maps RogueEssence.IntrudingBlobWaterStep to RogueElements.BlobWaterStep</description></item>
        /// <item><description>Legacy: Maps RogueEssence.LevelGen.MobSpawnSettingsStep to PMDC.LevelGen.MobSpawnSettingsStep</description></item>
        /// <item><description>Legacy: Maps RogueEssence.Data.UniversalActiveEffect to PMDC.Data.UniversalActiveEffect</description></item>
        /// </list>
        /// </remarks>
        public override Type BindToType(string assemblyName, string typeName)
        {
            //TODO: Remove in v1.1
            if (RogueEssence.Data.Serializer.OldVersion < new Version(0, 7, 0))
            {
                if (typeName.StartsWith("RogueEssence.LevelGen.FloorNameIDZoneStep"))
                {
                    assemblyName = assemblyName.Replace("RogueEssence", "PMDC");
                    typeName = typeName.Replace("RogueEssence.LevelGen.FloorNameIDZoneStep", "PMDC.LevelGen.FloorNameDropZoneStep");
                }
            }
            if (RogueEssence.Data.Serializer.OldVersion < new Version(0, 7, 21))
            {
                if (typeName.StartsWith("PMDC.Dungeon.AllyDifferentEvent"))
                    typeName = typeName.Replace("PMDC.Dungeon.AllyDifferentEvent", "PMDC.Dungeon.AlignmentDifferentEvent");
            }
            Type typeToDeserialize = Type.GetType(String.Format("{0}, {1}",
                typeName, assemblyName));

            if (typeToDeserialize == null)
            {
                //TODO: Remove in v1.1
                typeName = typeName.Replace("RefreshPreEvent", "ElementMobilityEvent");
                if (typeName.StartsWith("RogueEssence.IntrudingBlobWaterStep"))
                {
                    assemblyName = assemblyName.Replace("RogueEssence", "RogueElements");
                    typeName = typeName.Replace("RogueEssence.IntrudingBlobWaterStep", "RogueElements.BlobWaterStep");
                }
                if (typeName.StartsWith("RogueEssence.LevelGen.MobSpawnSettingsStep"))
                {
                    assemblyName = assemblyName.Replace("RogueEssence", "PMDC");
                    typeName = typeName.Replace("RogueEssence.LevelGen.MobSpawnSettingsStep", "PMDC.LevelGen.MobSpawnSettingsStep");
                }

                if (typeName.StartsWith("RogueEssence.Data.UniversalActiveEffect"))
                {
                    assemblyName = assemblyName.Replace("RogueEssence", "PMDC");
                    typeName = typeName.Replace("RogueEssence.Data.UniversalActiveEffect", "PMDC.Data.UniversalActiveEffect");
                }
                //typeName = typeName.Replace("From", "To");
                //assemblyName = assemblyName.Replace("From", "To");
                //then the type moved to a new namespace
                typeToDeserialize = Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
            }
            return typeToDeserialize;
        }
    }
}
