﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.Agents.BotBuilder.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.Agents.BotBuilder.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The method does not match the RouteHandler delegate definition..
        /// </summary>
        internal static string AttributeHandlerInvalid {
            get {
                return ResourceManager.GetString("AttributeHandlerInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The RouteAttribute.Selector method &apos;{0}&apos; does not match the RouteSelector delegate definition..
        /// </summary>
        internal static string AttributeSelectorInvalid {
            get {
                return ResourceManager.GetString("AttributeSelectorInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The RouteAttribute.Selector method &apos;{0}&apos; is not found..
        /// </summary>
        internal static string AttributeSelectorNotFound {
            get {
                return ResourceManager.GetString("AttributeSelectorNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An instance of IAccessTokenProvider not found for {0}.
        /// </summary>
        internal static string IAccessTokenProviderNotFound {
            get {
                return ResourceManager.GetString("IAccessTokenProviderNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The AgentApplication.Authentication property is unavailable because no user authentication handlers were configured..
        /// </summary>
        internal static string UserAuthenticationNotConfigured {
            get {
                return ResourceManager.GetString("UserAuthenticationNotConfigured", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to AgentApplication.UserAuthenticationFeature requires AgentApplicationOptions.Adapter set..
        /// </summary>
        internal static string UserAuthenticationRequiresAdapter {
            get {
                return ResourceManager.GetString("UserAuthenticationRequiresAdapter", resourceCulture);
            }
        }
    }
}
