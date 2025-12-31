using System;
using System.IO;
using RogueEssence.Data;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Xml.Serialization;
using RogueEssence.Content;
using Newtonsoft.Json;
using NLua;
using RogueElements;
using Newtonsoft.Json.Linq;
using RogueEssence.Dungeon;
using PMDC.Data;
using PMDC.Dungeon;
using RogueEssence.LevelGen;
using System.Collections;

namespace PMDC.Dev
{
    /// <summary>
    /// JSON converter for serializing and deserializing dictionaries with ItemFake keys and MobSpawn values.
    /// Converts the dictionary to/from an array format for JSON compatibility.
    /// </summary>
    /// <remarks>
    /// This converter enables Newtonsoft.Json to handle dictionary types that are not natively JSON-serializable
    /// by converting them to a list of tuples during serialization and reconstructing them during deserialization.
    /// </remarks>
    public class ItemFakeTableConverter : JsonConverter
    {
        /// <summary>
        /// Writes the dictionary as a JSON array of key-value tuples.
        /// </summary>
        /// <param name="writer">The JSON writer used to output the serialized data.</param>
        /// <param name="value">The dictionary object to serialize.</param>
        /// <param name="serializer">The JSON serializer instance for recursive serialization of nested objects.</param>
        /// <remarks>
        /// The method serializes each key-value pair as a tuple within a JSON array,
        /// allowing the dictionary to be represented in a JSON-compatible format.
        /// </remarks>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Dictionary<ItemFake, MobSpawn> dict = (Dictionary<ItemFake, MobSpawn>)value;
            writer.WriteStartArray();
            foreach (ItemFake item in dict.Keys)
            {
                serializer.Serialize(writer, (item, dict[item]));
            }
            writer.WriteEndArray();
        }

        /// <summary>
        /// Reads a JSON array and converts it back to a dictionary with ItemFake keys.
        /// </summary>
        /// <param name="reader">The JSON reader used to parse the input JSON.</param>
        /// <param name="objectType">The type of object being deserialized.</param>
        /// <param name="existingValue">The existing value (if any) to populate; typically null for this converter.</param>
        /// <param name="serializer">The JSON serializer instance for recursive deserialization of nested objects.</param>
        /// <returns>A deserialized Dictionary&lt;ItemFake, MobSpawn&gt; containing the key-value pairs from the JSON array.</returns>
        /// <remarks>
        /// The method deserializes a JSON array of tuples into a dictionary. Each tuple in the array
        /// is treated as a key-value pair, where the first element becomes the dictionary key.
        /// </remarks>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Dictionary<ItemFake, MobSpawn> dict = new Dictionary<ItemFake, MobSpawn>();

            JArray jArray = JArray.Load(reader);
            List<(ItemFake, MobSpawn)> container = new List<(ItemFake, MobSpawn)>();
            serializer.Populate(jArray.CreateReader(), container);

            foreach ((ItemFake, MobSpawn) item in container)
                dict[item.Item1] = item.Item2;

            return dict;
        }

        /// <summary>
        /// Determines whether this converter can handle the specified type.
        /// </summary>
        /// <param name="objectType">The type to check for converter compatibility.</param>
        /// <returns>
        /// true if the type is Dictionary&lt;ItemFake, MobSpawn&gt;; otherwise, false.
        /// </returns>
        /// <remarks>
        /// This method is called by the JSON serializer to determine if this converter
        /// should be used for a given type during serialization/deserialization.
        /// </remarks>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<ItemFake, MobSpawn>);
        }
    }


    /// <summary>
    /// JSON converter for serializing and deserializing dictionaries with MonsterID keys and Mobility values.
    /// Converts the dictionary to/from an array format for JSON compatibility.
    /// </summary>
    /// <remarks>
    /// This converter enables Newtonsoft.Json to handle dictionary types that are not natively JSON-serializable
    /// by converting them to a list of tuples during serialization and reconstructing them during deserialization.
    /// Used for terrain mobility settings that define how different monster species interact with specific terrain types.
    /// </remarks>
    public class MobilityTableConverter : JsonConverter
    {
        /// <summary>
        /// Writes the dictionary as a JSON array of key-value tuples.
        /// </summary>
        /// <param name="writer">The JSON writer used to output the serialized data.</param>
        /// <param name="value">The dictionary object to serialize.</param>
        /// <param name="serializer">The JSON serializer instance for recursive serialization of nested objects.</param>
        /// <remarks>
        /// The method serializes each key-value pair as a tuple within a JSON array,
        /// allowing the dictionary to be represented in a JSON-compatible format.
        /// Each tuple consists of a MonsterID and its corresponding Mobility data.
        /// </remarks>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Dictionary<MonsterID, TerrainData.Mobility> dict = (Dictionary<MonsterID, TerrainData.Mobility>)value;
            writer.WriteStartArray();
            foreach (MonsterID item in dict.Keys)
            {
                serializer.Serialize(writer, (item, dict[item]));
            }
            writer.WriteEndArray();
        }

        /// <summary>
        /// Reads a JSON array and converts it back to a dictionary with MonsterID keys.
        /// </summary>
        /// <param name="reader">The JSON reader used to parse the input JSON.</param>
        /// <param name="objectType">The type of object being deserialized.</param>
        /// <param name="existingValue">The existing value (if any) to populate; typically null for this converter.</param>
        /// <param name="serializer">The JSON serializer instance for recursive deserialization of nested objects.</param>
        /// <returns>
        /// A deserialized Dictionary&lt;MonsterID, TerrainData.Mobility&gt; containing the monster mobility mappings
        /// parsed from the JSON array.
        /// </returns>
        /// <remarks>
        /// The method deserializes a JSON array of tuples into a dictionary. Each tuple in the array
        /// is treated as a key-value pair, where the first element (MonsterID) becomes the dictionary key
        /// and the second element (Mobility) becomes the value.
        /// </remarks>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Dictionary<MonsterID, TerrainData.Mobility> dict = new Dictionary<MonsterID, TerrainData.Mobility>();

            JArray jArray = JArray.Load(reader);
            List<(MonsterID, TerrainData.Mobility)> container = new List<(MonsterID, TerrainData.Mobility)>();
            serializer.Populate(jArray.CreateReader(), container);

            foreach ((MonsterID, TerrainData.Mobility) item in container)
                dict[item.Item1] = item.Item2;

            return dict;
        }

        /// <summary>
        /// Determines whether this converter can handle the specified type.
        /// </summary>
        /// <param name="objectType">The type to check for converter compatibility.</param>
        /// <returns>
        /// true if the type is Dictionary&lt;MonsterID, TerrainData.Mobility&gt;; otherwise, false.
        /// </returns>
        /// <remarks>
        /// This method is called by the JSON serializer to determine if this converter
        /// should be used for a given type during serialization/deserialization.
        /// </remarks>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<MonsterID, TerrainData.Mobility>);
        }
    }
}
