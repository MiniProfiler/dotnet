using System;
using System.Reflection;

namespace Subtext.TestLibrary
{
    /// <summary>
    /// Helper class to simplify common reflection tasks.
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Returns the value of the private member specified.
        /// </summary>
        /// <typeparam name="T">The result type</typeparam>
        /// <param name="fieldName">Name of the member.</param>
        /// <param name="type">Type of the member.</param>
        public static T GetStaticFieldValue<T>(string fieldName, Type type)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null)
            {
                return (T)field.GetValue(type);
            }
            return default(T);
        }

        /// <summary>
        /// Returns the value of the private member specified.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="source">The object that contains the member.</param>
        /// <param name="value">The value to set the member to.</param>
        /// <exception cref="ArgumentException">Thrown when the field isn't found.</exception>
        public static void SetPrivateInstanceFieldValue(string memberName, object source, object value)
        {
            FieldInfo field = source.GetType().GetField(memberName, BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new ArgumentException(string.Format("Could not find the private instance field '{0}'", memberName));

            field.SetValue(source, value);
        }

        /// <summary>
        /// Creates a new instance of a type
        /// </summary>
        /// <param name="typeName">The type to create.</param>
        /// <param name="constructorArgumentTypes">The constructor argument types to pass in.</param>
        /// <param name="constructorParameterValues">The constructor arguments to pass in.</param>
        /// <returns>An instantiated object.</returns>
        public static object Instantiate(string typeName, Type[] constructorArgumentTypes, params object[] constructorParameterValues)
        {
            return Instantiate(Type.GetType(typeName, true), constructorArgumentTypes, constructorParameterValues);
        }

        /// <summary>
        /// Creates a new instance of a type
        /// </summary>
        /// <param name="type">The type to create.</param>
        /// <param name="constructorArgumentTypes">The constructor argument types to pass in.</param>
        /// <param name="constructorParameterValues">The constructor arguments to pass in.</param>
        /// <returns>An instantiated object.</returns>
        public static object Instantiate(Type type, Type[] constructorArgumentTypes, params object[] constructorParameterValues)
        {
            ConstructorInfo constructor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, constructorArgumentTypes, null);
            return constructor.Invoke(constructorParameterValues);
        }
    }
}
