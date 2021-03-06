﻿using Jasily.Framework.ConsoleEngine.Mappers;
using System;
using System.Collections.Generic;

namespace Jasily.Framework.ConsoleEngine.Converters
{
    public struct ConverterAgent
    {
        private readonly IReadOnlyDictionary<Type, object> defaultValue;
        private readonly ConvertersMapper convertersMapper;

        public ConverterAgent(ConvertersMapper convertersMapper, IReadOnlyDictionary<Type, object> defaultValue)
        {
            this.defaultValue = defaultValue;
            this.convertersMapper = convertersMapper;
        }

        public bool CanConvert(Type to)
        {
            if (to == typeof(string)) return true;
            if (to.IsEnum) return true;
            if (this.convertersMapper[to] != null) return true;
            if (to.IsGenericType)
            {
                var genericTypeDef = to.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(Nullable<>))
                {
                    return this.CanConvert(to.GetGenericArguments()[0]);
                }
            }
            return this.defaultValue.ContainsKey(to);
        }

        public bool Convert(Type to, string input, out object output)
        {
            if (to == typeof(string))
            {
                output = input;
                return true;
            }

            var converter = this.convertersMapper[to];
            if (converter == null)
            {
                if (to.IsGenericType)
                {
                    var genericTypeDef = to.GetGenericTypeDefinition();
                    if (genericTypeDef == typeof(Nullable<>))
                    {
                        converter = this.convertersMapper.NullableConverter;
                    }
                }
            }

            if (converter != null) return converter.Convert(to, input, out output);

            if (this.defaultValue.TryGetValue(to, out output)) return true;

            return this.convertersMapper.EnumConverter.Convert(to, input, out output);
        }

        public string GetVaildInput(Type to)
        {
            var converter = this.convertersMapper[to];
            if (converter != null)
            {
                return converter.GetVaildInput(to);
            }
            return this.convertersMapper.EnumConverter.GetVaildInput(to);
        }
    }
}