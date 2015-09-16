using System.Collections.Generic;

namespace Esp.Net.Examples.ReactiveModel.Common.Model.Entities.Fields
{
    public interface IField<out T> : IField
    {
        T Value { get; }
    }

    public interface IField
    {
        bool IsEnabled { get; }
        bool IsValid { get; }
        bool HasValue { get; }
    }

    public abstract class Field : IField
    {
        public bool IsEnabled { get; set; }

        public bool IsValid { get; set; }

        public abstract bool HasValue { get; }
    }

    public class Field<T> : Field, IField<T>
    {
        public T Value { get; set; }

        public override bool HasValue
        {
            get
            {
                return !EqualityComparer<T>.Default.Equals(Value, default(T));        
            }
        }
    }
}