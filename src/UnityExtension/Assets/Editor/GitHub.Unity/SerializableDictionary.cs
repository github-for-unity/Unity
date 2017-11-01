using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GitHub.Unity
{
    //http://answers.unity3d.com/answers/809221/view.html

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();

        [SerializeField]
        private List<TValue> values = new List<TValue>();

        // save the dictionary to lists
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        // load dictionary from lists
        public void OnAfterDeserialize()
        {
            this.Clear();

            if (keys.Count != values.Count)
                throw new Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable.", keys.Count, values.Count));

            for (int i = 0; i < keys.Count; i++)
                this.Add(keys[i], values[i]);
        }
    }

    [Serializable]
    public class ArrayContainer
    {
        [SerializeField]
        public object[] Values = new object[0];
    }

    [Serializable]
    public class SerializableNestedDictionary<TKey, TValue> : Dictionary<TKey, Dictionary<TKey, TValue>>, ISerializationCallbackReceiver
    {
        [SerializeField] private TKey[] keys = new TKey[0];
        [SerializeField] private ArrayContainer[] subKeys = new ArrayContainer[0];
        [SerializeField] private ArrayContainer[] subKeyValues = new ArrayContainer[0];

        // save the dictionary to lists
        public void OnBeforeSerialize()
        {
            var keyList = new List<TKey>();
            var subKeysList = new List<ArrayContainer>();
            var subKeysValuesList = new List<ArrayContainer>();

            foreach (var pair in this)
            {
                var pairKey = pair.Key;
                keyList.Add(pairKey);

                var serializeSubKeys = new List<TKey>();
                var serializeSubKeyValues = new List<TValue>();

                var subDictionary = pair.Value;
                foreach (var subPair in subDictionary)
                {
                    serializeSubKeys.Add(subPair.Key);
                    serializeSubKeyValues.Add(subPair.Value);
                }

                subKeysList.Add(new ArrayContainer { Values = serializeSubKeys.Cast<object>().ToArray() });
                subKeysValuesList.Add(new ArrayContainer { Values = serializeSubKeyValues.Cast<object>().ToArray() });
            }

            keys = keyList.ToArray();
            subKeys = subKeysList.ToArray();
            subKeyValues = subKeysValuesList.ToArray();
        }

        // load dictionary from lists
        public void OnAfterDeserialize()
        {
            this.Clear();
        }
    }
}