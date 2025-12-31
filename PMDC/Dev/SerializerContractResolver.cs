using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PMDC.Dungeon;
using RogueEssence.Dev;
using RogueEssence.LevelGen;

namespace PMDC.Dev
{
    /// <summary>
    /// Custom JSON contract resolver that handles serialization of game data types.
    /// Filters out members marked with <see cref="NonSerializedAttribute"/> and ensures proper property binding.
    /// </summary>
    /// <remarks>
    /// This resolver extends <see cref="DefaultContractResolver"/> to provide custom serialization behavior
    /// for PMDC game data. It removes any fields or properties marked with <see cref="NonSerializedAttribute"/>
    /// from the JSON output and ensures all remaining properties are both readable and writable.
    /// </remarks>
    public class SerializerContractResolver : DefaultContractResolver
    {
        /// <summary>
        /// Creates a list of JSON properties for the specified type, excluding non-serialized members.
        /// </summary>
        /// <remarks>
        /// This method retrieves all serializable members from the specified type using reflection,
        /// filters out any members marked with <see cref="NonSerializedAttribute"/>, and converts
        /// them to JSON properties. All resulting properties are configured to be both readable and writable.
        /// </remarks>
        /// <param name="type">The type to create properties for.</param>
        /// <param name="memberSerialization">The member serialization mode.</param>
        /// <returns>A list of JSON properties suitable for serialization of the specified type.</returns>
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            List<MemberInfo> fields = type.GetSerializableMembers();

            // Remove members marked with NonSerializedAttribute
            for (int ii = fields.Count - 1; ii >= 0; ii--)
            {
                if (fields[ii].GetCustomAttributes(typeof(NonSerializedAttribute), false).Length > 0)
                    fields.RemoveAt(ii);
            }

            List<JsonProperty> props = fields.Select(f => CreateProperty(f, memberSerialization))
                .ToList();
            props.ForEach(p => { p.Writable = true; p.Readable = true; });
            return props;

        }

        // NOTE: The following override was attempted but did not work as expected; kept for reference.
        // TODO: Investigate why custom converter resolution doesn't work as expected.
        // protected override JsonConverter ResolveContractConverter(Type objectType)
        // {
        //     if (objectType.Equals(typeof(ElementMobilityEvent)))
        //         return new ElementMobilityEventConverter();
        //     return base.ResolveContractConverter(objectType);
        // }
    }
}
