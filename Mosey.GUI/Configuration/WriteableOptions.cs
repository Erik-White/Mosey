﻿using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Mosey.GUI.Models;

namespace Mosey.GUI.Configuration
{
    public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private readonly IOptionsSnapshot<T> _options;
        private readonly string _name;

        public WritableOptions(IOptionsSnapshot<T> options, string name)
        {
            _options = options;
            _name = name;
        }

        public T Value => _options.Value;
        public T Get(string name) => _options.Get(name);

        public void Update(Action<T> applyChanges)
        {
            var physicalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _name);

            var optionsInstance = JsonSerializer.Deserialize<T>(File.ReadAllText(physicalPath)) ?? new T();

            applyChanges(optionsInstance);

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            File.WriteAllText(physicalPath, JsonSerializer.Serialize(optionsInstance, jsonOptions));
        }
    }
}
