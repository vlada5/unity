﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Xml;
using Microsoft.Practices.Unity.Configuration;
using Microsoft.Practices.Unity.Configuration.ConfigurationHelpers;
using Microsoft.Practices.Unity.InterceptionExtension.Configuration.Properties;
using Microsoft.Practices.Unity.Utility;

namespace Microsoft.Practices.Unity.InterceptionExtension.Configuration
{
    /// <summary>
    /// Configuration elmement for specifying 
    /// interception behaviors for a type.
    /// </summary>
    public class InterceptionBehaviorElement : InjectionMemberElement
    {
        private const string TypeNamePropertyName = "type";
        private const string NamePropertyName = "name";
        private const string IsDefaultForTypePropertyName = "isDefaultForType";

        /// <summary>
        /// Type of behavior to add.
        /// </summary>
        [ConfigurationProperty(TypeNamePropertyName)]
        public string TypeName
        {
            get { return (string) base[TypeNamePropertyName]; }
            set { base[TypeNamePropertyName] = value; }
        }

        /// <summary>
        /// Name of behavior to resolve.
        /// </summary>
        [ConfigurationProperty(NamePropertyName)]
        public string Name
        {
            get { return (string) base[NamePropertyName]; }
            set { base[NamePropertyName] = value; }
        }

        /// <summary>
        /// Should this behavior be configured as a default behavior for this type, or
        /// specifically for this type/name pair only?
        /// </summary>
        [ConfigurationProperty(IsDefaultForTypePropertyName, IsRequired = false, DefaultValue = false)]
        public bool IsDefaultForType
        {
            get { return (bool) base[IsDefaultForTypePropertyName]; }
            set { base[IsDefaultForTypePropertyName] = value; }
        }


        /// <summary>
        /// Reads XML from the configuration file.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader"/> that reads from the configuration file.
        ///                 </param><param name="serializeCollectionKey">true to serialize only the collection key properties; otherwise, false.
        ///                 </param><exception cref="T:System.Configuration.ConfigurationErrorsException">The element to read is locked.
        ///                     - or -
        ///                     An attribute of the current node is not recognized.
        ///                     - or -
        ///                     The lock status of the current node cannot be determined.  
        ///                 </exception>
        protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
        {
            base.DeserializeElement(reader, serializeCollectionKey);

            GuardHasRequiredAttributes();
        }

        /// <summary>
        /// Each element must have a unique key, which is generated by the subclasses.
        /// </summary>
        public override string Key
        {
            get
            {
                if(string.IsNullOrEmpty(TypeName))
                {
                    return "interceptionBehavior:IInterceptionBehavior:" + Name;
                }
                return "interceptionBehavior:" + TypeName + ":" + Name;
            }
        }

        /// <summary>
        /// Write the contents of this element to the given <see cref="XmlWriter"/>.
        /// </summary>
        /// <remarks>The caller of this method has already written the start element tag before
        /// calling this method, so deriving classes only need to write the element content, not
        /// the start or end tags.</remarks>
        /// <param name="writer">Writer to send XML content to.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods",
            Justification = "Validation done by Guard class")]
        public override void SerializeContent(XmlWriter writer)
        {
            Guard.ArgumentNotNull(writer, "writer");
            writer.WriteAttributeIfNotEmpty(NamePropertyName, Name);
            writer.WriteAttributeIfNotEmpty(TypeNamePropertyName, TypeName);
            if(IsDefaultForType)
            {
                writer.WriteAttributeString(IsDefaultForTypePropertyName, IsDefaultForType.ToString());
            }
        }

        /// <summary>
        /// Return the set of <see cref="InjectionMember"/>s that are needed
        /// to configure the container according to this configuration element.
        /// </summary>
        /// <param name="container">Container that is being configured.</param>
        /// <param name="fromType">Type that is being registered.</param>
        /// <param name="toType">Type that <paramref name="fromType"/> is being mapped to.</param>
        /// <param name="name">Name this registration is under.</param>
        /// <returns>One or more <see cref="InjectionMember"/> objects that should be
        /// applied to the container registration.</returns>
        public override IEnumerable<InjectionMember> GetInjectionMembers(IUnityContainer container, Type fromType, Type toType, string name)
        {
            Type behaviorType = TypeResolver.ResolveTypeWithDefault(TypeName, typeof (IInterceptionBehavior));
            GuardBehaviorType(behaviorType);

            if(IsDefaultForType)
            {
                return new[] {new DefaultInterceptionBehavior(behaviorType, Name)};
            }
            return new[] {new InterceptionBehavior(behaviorType, Name)};
        }

        private void GuardHasRequiredAttributes()
        {
            if(string.IsNullOrEmpty(TypeName) &&
                string.IsNullOrEmpty(Name))
            {
                throw new ConfigurationErrorsException(Resources.MustHaveAtLeastOneBehaviorAttribute);
            }
        }

        private void GuardBehaviorType(Type resolvedType)
        {
            if(!typeof(IInterceptionBehavior).IsAssignableFrom(resolvedType))
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture,
                        Resources.ExceptionResolvedTypeNotCompatible,
                        TypeName, resolvedType.FullName, typeof (IInterceptionBehavior).FullName));
            }
        }
    }
}
