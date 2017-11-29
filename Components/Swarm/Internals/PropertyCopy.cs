    //"Miscellaneous Utility Library" Software Licence

    //Version 1.0

    //Copyright(c) 2004-2008 Jon Skeet and Marc Gravell.
    //All rights reserved.

    //Redistribution and use in source and binary forms, with or without
    //modification, are permitted provided that the following conditions
    //are met:

    //1. Redistributions of source code must retain the above copyright
    //notice, this list of conditions and the following disclaimer.

    //2. Redistributions in binary form must reproduce the above copyright
    //notice, this list of conditions and the following disclaimer in the
    //documentation and/or other materials provided with the distribution.

    //3. The end-user documentation included with the redistribution, if
    //any, must include the following acknowledgment:

    //"This product includes software developed by Jon Skeet
    //and Marc Gravell.Contact skeet@pobox.com, or see
    //http://www.pobox.com/~skeet/)."

    //Alternately, this acknowledgment may appear in the software itself,
    //if and wherever such third-party acknowledgments normally appear.

    //4. The name "Miscellaneous Utility Library" must not be used to endorse
    //or promote products derived from this software without prior written
    //permission.For written permission, please contact skeet @pobox.com.

    //5. Products derived from this software may not be called
    //"Miscellaneous Utility Library", nor may "Miscellaneous Utility Library"
    //appear in their name, without prior written permission of Jon Skeet.

    //THIS SOFTWARE IS PROVIDED "AS IS" AND ANY EXPRESSED OR IMPLIED
    //WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
    //MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
    //IN NO EVENT SHALL JON SKEET BE LIABLE FOR ANY DIRECT, INDIRECT,
    //INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
    //BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
    //LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
    //CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
    //LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
    //ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
    //POSSIBILITY OF SUCH DAMAGE. 

    //http://www.yoda.arachsys.com/csharp/miscutil/index.html

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Trinity.Components.Swarm.Internals
{
    /// <summary>
    /// Non-generic class allowing properties to be copied from one instance
    /// to another existing instance of a potentially different type.
    /// </summary>
    public static class PropertyCopy
    {
        /// <summary>
        /// Copies all public, readable properties from the source object to the
        /// target. The target type does not have to have a parameterless constructor,
        /// as no new instance needs to be created.
        /// </summary>
        /// <remarks>Only the properties of the source and target types themselves
        /// are taken into account, regardless of the actual types of the arguments.</remarks>
        /// <typeparam name="TSource">Type of the source</typeparam>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="source">Source to copy properties from</param>
        /// <param name="target">Target to copy properties to</param>
        public static void Copy<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class
        {
            PropertyCopier<TSource, TTarget>.Copy(source, target);
        }
    }

    /// <summary>
    /// Generic class which copies to its target type from a source
    /// type specified in the Copy method. The types are specified
    /// separately to take advantage of type inference on generic
    /// method arguments.
    /// </summary>
    public static class PropertyCopy<TTarget> where TTarget : class, new()
    {
        /// <summary>
        /// Copies all readable properties from the source to a new instance
        /// of TTarget.
        /// </summary>
        public static TTarget CopyFrom<TSource>(TSource source) where TSource : class
        {
            return PropertyCopier<TSource, TTarget>.Copy(source);
        }
    }

    /// <summary>
    /// Static class to efficiently store the compiled delegate which can
    /// do the copying. We need a bit of work to ensure that exceptions are
    /// appropriately propagated, as the exception is generated at type initialization
    /// time, but we wish it to be thrown as an ArgumentException.
    /// Note that this type we do not have a constructor constraint on TTarget, because
    /// we only use the constructor when we use the form which creates a new instance.
    /// </summary>
    internal static class PropertyCopier<TSource, TTarget>
    {
        /// <summary>
        /// Delegate to create a new instance of the target type given an instance of the
        /// source type. This is a single delegate from an expression tree.
        /// </summary>
        private static readonly Func<TSource, TTarget> creator;

        /// <summary>
        /// List of properties to grab values from. The corresponding targetProperties 
        /// list contains the same properties in the target type. Unfortunately we can't
        /// use expression trees to do this, because we basically need a sequence of statements.
        /// We could build a DynamicMethod, but that's significantly more work :) Please mail
        /// me if you really need this...
        /// </summary>
        private static readonly List<PropertyInfo> sourceProperties = new List<PropertyInfo>();
        private static readonly List<PropertyInfo> targetProperties = new List<PropertyInfo>();
        private static readonly Exception initializationException;

        internal static TTarget Copy(TSource source)
        {
            if (initializationException != null)
            {
                throw initializationException;
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return creator(source);
        }

        internal static void Copy(TSource source, TTarget target)
        {
            if (initializationException != null)
            {
                throw initializationException;
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            for (int i = 0; i < sourceProperties.Count; i++)
            {
                targetProperties[i].SetValue(target, sourceProperties[i].GetValue(source, null), null);
            }

        }

        static PropertyCopier()
        {
            try
            {
                creator = BuildCreator();
                initializationException = null;
            }
            catch (Exception e)
            {
                creator = null;
                initializationException = e;
            }
        }

        private static Func<TSource, TTarget> BuildCreator()
        {
            ParameterExpression sourceParameter = Expression.Parameter(typeof(TSource), "source");
            var bindings = new List<MemberBinding>();
            foreach (PropertyInfo sourceProperty in typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!sourceProperty.CanRead)
                {
                    continue;
                }
                PropertyInfo targetProperty = typeof(TTarget).GetProperty(sourceProperty.Name);
                if (targetProperty == null)
                {
                    throw new ArgumentException("Property " + sourceProperty.Name + " is not present and accessible in " + typeof(TTarget).FullName);
                }
                if (targetProperty.GetCustomAttribute(typeof (IgnoreDataMemberAttribute)) != null)
                {
                    continue;
                }
                if (!targetProperty.CanWrite)
                {
                    throw new ArgumentException("Property " + sourceProperty.Name + " is not writable in " + typeof(TTarget).FullName);
                }
                if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
                {
                    throw new ArgumentException("Property " + sourceProperty.Name + " is static in " + typeof(TTarget).FullName);
                }
                if (!targetProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                {
                    throw new ArgumentException("Property " + sourceProperty.Name + " has an incompatible type in " + typeof(TTarget).FullName);
                }
                bindings.Add(Expression.Bind(targetProperty, Expression.Property(sourceParameter, sourceProperty)));
                sourceProperties.Add(sourceProperty);
                targetProperties.Add(targetProperty);
            }
            Expression initializer = Expression.MemberInit(Expression.New(typeof(TTarget)), bindings);
            return Expression.Lambda<Func<TSource, TTarget>>(initializer, sourceParameter).Compile();
        }
    }
}