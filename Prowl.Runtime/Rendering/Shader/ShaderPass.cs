// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;

using Veldrid;

namespace Prowl.Runtime
{
    public struct ShaderPassDescription
    {
        public Dictionary<string, string>? Tags;
        public BlendStateDescription? BlendState;
        public DepthStencilStateDescription? DepthStencilState;
        public FaceCullMode? CullingMode;
        public bool? DepthClipEnabled;

        public Dictionary<string, HashSet<string>>? Keywords;


        private static void SetDefault<T>(ref T? currentValue, T? defaultValue)
        {
            if (currentValue == null && defaultValue != null)
                currentValue = defaultValue;
        }

        public void ApplyDefaults(ShaderPassDescription defaults)
        {
            SetDefault(ref Tags, defaults.Tags);
            SetDefault(ref BlendState, defaults.BlendState);
            SetDefault(ref DepthStencilState, defaults.DepthStencilState);
            SetDefault(ref CullingMode, defaults.CullingMode);
            SetDefault(ref DepthClipEnabled, defaults.DepthClipEnabled);
            SetDefault(ref Keywords, defaults.Keywords);
        }
    }

    public sealed class ShaderPass : ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private string _name;

        [SerializeField, HideInInspector]
        private Dictionary<string, string> _tags;

        [SerializeField, HideInInspector]
        private BlendStateDescription _blend;

        [SerializeField, HideInInspector]
        private DepthStencilStateDescription _depthStencilState;

        [SerializeField, HideInInspector]
        private FaceCullMode _cullMode = FaceCullMode.Back;

        [SerializeField, HideInInspector]
        private bool _depthClipEnabled = true;

        [NonSerialized]
        private Dictionary<string, HashSet<string>> _keywords;

        [NonSerialized]
        private Dictionary<KeywordState, ShaderVariant> _variants;


        /// <summary>
        /// The name to identify this <see cref="ShaderPass"/>
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// The tags to identify this <see cref="ShaderPass"/>
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> Tags => _tags;

        /// <summary>
        /// The blending options to use when rendering this <see cref="ShaderPass"/>
        /// </summary>
        public BlendStateDescription Blend => _blend;

        /// <summary>
        /// The depth stencil state to use when rendering this <see cref="ShaderPass"/>
        /// </summary>
        public DepthStencilStateDescription DepthStencilState => _depthStencilState;

        /// <summary>
        /// Pass face culling mode.
        /// </summary>
        public FaceCullMode CullMode => _cullMode;

        /// <summary>
        /// Pass depth clip mode.
        /// </summary>
        public bool DepthClipEnabled => _depthClipEnabled;


        public IEnumerable<KeyValuePair<string, HashSet<string>>> Keywords => _keywords;
        public IEnumerable<KeyValuePair<KeywordState, ShaderVariant>> Variants => _variants;


        private ShaderPass() { }

        public ShaderPass(string name, ShaderPassDescription description, ShaderVariant[] variants)
        {
            this._name = name;

            this._tags = description.Tags ?? new();
            this._blend = description.BlendState ?? BlendStateDescription.SingleOverrideBlend;
            this._depthStencilState = description.DepthStencilState ?? DepthStencilStateDescription.DepthOnlyLessEqual;
            this._cullMode = description.CullingMode ?? FaceCullMode.Back;
            this._depthClipEnabled = description.DepthClipEnabled ?? true;
            this._keywords = description.Keywords ?? new() { { string.Empty, [string.Empty] } };

            this._variants = new();

            foreach (var variant in variants)
                this._variants[variant.VariantKeywords] = variant;
        }

        public ShaderVariant GetVariant(KeywordState? keywordID = null)
            => _variants[ValidateKeyword(keywordID ?? KeywordState.Empty)];

        public bool TryGetVariant(KeywordState? keywordID, out ShaderVariant? variant)
            => _variants.TryGetValue(keywordID ?? KeywordState.Empty, out variant);

        public bool HasTag(string tag, string? tagValue = null)
        {
            if (_tags.TryGetValue(tag, out string value))
                return tagValue == null || value == tagValue;

            return false;
        }

        public KeywordState ValidateKeyword(KeywordState key)
        {
            KeywordState combinedKey = new();

            foreach (var definition in _keywords)
            {
                string defaultValue = definition.Value.First();
                string value = key.GetKey(definition.Key, defaultValue);
                value = definition.Value.Contains(value) ? value : defaultValue;

                combinedKey.SetKey(definition.Key, value);
            }

            return combinedKey;
        }


        [SerializeField, HideInInspector]
        private string[] _serializedKeywordKeys;

        [SerializeField, HideInInspector]
        private string[][] _serializedKeywordValues;


        [SerializeField, HideInInspector]
        private ShaderVariant[] _serializedVariants;

        public void OnBeforeSerialize()
        {
            _serializedKeywordKeys = _keywords.Keys.ToArray();
            _serializedKeywordValues = _keywords.Values.Select(x => x.ToArray()).ToArray();

            _serializedVariants = _variants.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
            _keywords = new();

            for (int i = 0; i < _serializedKeywordKeys.Length; i++)
                _keywords.Add(_serializedKeywordKeys[i], new(_serializedKeywordValues[i]));

            _variants = new();

            foreach (var variant in _serializedVariants)
                _variants.Add(variant.VariantKeywords, variant);
        }
    }
}
