using System.Collections.Generic;
using System.Reflection;
using System.ArrayExtensions;

namespace System
{
    /// <summary>
    /// Provides extension methods for deep copying objects using reflection.
    /// Performs recursive cloning of object graphs while preserving reference cycles.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// The MemberwiseClone method retrieved via reflection for creating shallow copies of objects.
        /// </summary>
        private static readonly MethodInfo CloneMethod = typeof(Object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Determines whether the specified type is a primitive type or string.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is primitive or string; otherwise, false.</returns>
        public static bool IsPrimitive(this Type type)
        {
            if (type == typeof(String)) return true;
            return (type.IsValueType & type.IsPrimitive);
        }

        /// <summary>
        /// Creates a deep copy of the specified object, preserving reference cycles.
        /// </summary>
        /// <param name="originalObject">The object to copy.</param>
        /// <returns>A deep copy of the object, or null if the original was null.</returns>
        public static Object Copy(this Object originalObject)
        {
            return InternalCopy(originalObject, new Dictionary<Object, Object>(new ReferenceEqualityComparer()));
        }

        /// <summary>
        /// Recursively copies an object and its fields, tracking visited objects to preserve reference cycles.
        /// </summary>
        /// <param name="originalObject">The object to copy.</param>
        /// <param name="visited">Dictionary tracking already-copied objects by reference identity.</param>
        /// <returns>A deep copy of the object, or null if the original was null.</returns>
        private static Object InternalCopy(Object originalObject, IDictionary<Object, Object> visited)
        {
            if (originalObject == null) return null;
            var typeToReflect = originalObject.GetType();
            if (IsPrimitive(typeToReflect)) return originalObject;
            if (visited.ContainsKey(originalObject)) return visited[originalObject];
            if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
            var cloneObject = CloneMethod.Invoke(originalObject, null);
            if (typeToReflect.IsArray)
            {
                var arrayType = typeToReflect.GetElementType();
                if (IsPrimitive(arrayType) == false)
                {
                    Array clonedArray = (Array)cloneObject;
                    clonedArray.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
                }

            }
            visited.Add(originalObject, cloneObject);
            CopyFields(originalObject, visited, cloneObject, typeToReflect);
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
            return cloneObject;
        }

        /// <summary>
        /// Recursively copies private fields from base types up the inheritance hierarchy.
        /// </summary>
        /// <param name="originalObject">The original object being copied.</param>
        /// <param name="visited">Dictionary tracking already-copied objects by reference identity.</param>
        /// <param name="cloneObject">The cloned object to populate with copied field values.</param>
        /// <param name="typeToReflect">The current type in the inheritance hierarchy being processed.</param>
        private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect)
        {
            if (typeToReflect.BaseType != null)
            {
                RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
                CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
            }
        }

        /// <summary>
        /// Copies all fields of a specified type from the original object to the clone, recursively copying non-primitive field values.
        /// </summary>
        /// <param name="originalObject">The original object to copy fields from.</param>
        /// <param name="visited">Dictionary tracking already-copied objects by reference identity.</param>
        /// <param name="cloneObject">The cloned object to populate with copied field values.</param>
        /// <param name="typeToReflect">The type whose fields should be copied.</param>
        /// <param name="bindingFlags">Binding flags specifying which fields to retrieve; defaults to all instance and static fields.</param>
        /// <param name="filter">Optional predicate to filter which fields should be copied.</param>
        private static void CopyFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool> filter = null)
        {
            foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
            {
                if (filter != null && filter(fieldInfo) == false) continue;
                if (IsPrimitive(fieldInfo.FieldType)) continue;
                var originalFieldValue = fieldInfo.GetValue(originalObject);
                var clonedFieldValue = InternalCopy(originalFieldValue, visited);
                fieldInfo.SetValue(cloneObject, clonedFieldValue);
            }
        }

        /// <summary>
        /// Creates a deep copy of the specified object with strong typing.
        /// </summary>
        /// <typeparam name="T">The type of the object to copy.</typeparam>
        /// <param name="original">The object to copy.</param>
        /// <returns>A deep copy of the object.</returns>
        public static T Copy<T>(this T original)
        {
            return (T)Copy((Object)original);
        }
    }

    /// <summary>
    /// Compares objects by reference identity rather than value equality.
    /// Used by deep copy operations to track already-visited objects and preserve cycles.
    /// </summary>
    public class ReferenceEqualityComparer : EqualityComparer<Object>
    {
        /// <inheritdoc/>
        public override bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        /// <inheritdoc/>
        public override int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }

    namespace ArrayExtensions
    {
        /// <summary>
        /// Provides extension methods for multi-dimensional array traversal.
        /// </summary>
        public static class ArrayExtensions
        {
            /// <summary>
            /// Executes an action for each element in a multi-dimensional array.
            /// </summary>
            /// <param name="array">The array to traverse.</param>
            /// <param name="action">The action to execute, receiving the array and current indices.</param>
            public static void ForEach(this Array array, Action<Array, int[]> action)
            {
                if (array.LongLength == 0) return;
                ArrayTraverse walker = new ArrayTraverse(array);
                do action(array, walker.Position);
                while (walker.Step());
            }
        }

        /// <summary>
        /// Helper class for traversing multi-dimensional arrays by incrementing indices in row-major order.
        /// </summary>
        internal class ArrayTraverse
        {
            /// <summary>
            /// The current position indices in the multi-dimensional array.
            /// </summary>
            public int[] Position;

            /// <summary>
            /// The maximum valid index for each dimension of the array.
            /// </summary>
            private int[] maxLengths;

            /// <summary>
            /// Initializes a new array traverser for the specified array.
            /// </summary>
            /// <param name="array">The array to traverse.</param>
            public ArrayTraverse(Array array)
            {
                maxLengths = new int[array.Rank];
                for (int i = 0; i < array.Rank; ++i)
                {
                    maxLengths[i] = array.GetLength(i) - 1;
                }
                Position = new int[array.Rank];
            }

            /// <summary>
            /// Advances to the next position in the multi-dimensional array traversal.
            /// </summary>
            /// <returns>True if there are more elements to traverse; false if traversal is complete.</returns>
            public bool Step()
            {
                for (int i = 0; i < Position.Length; ++i)
                {
                    if (Position[i] < maxLengths[i])
                    {
                        Position[i]++;
                        for (int j = 0; j < i; j++)
                        {
                            Position[j] = 0;
                        }
                        return true;
                    }
                }
                return false;
            }
        }
    }

}