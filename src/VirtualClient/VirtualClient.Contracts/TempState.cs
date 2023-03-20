// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using Newtonsoft.Json;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Information on the overall state of execution.
    /// </summary>
    public class TempState : State
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TempState"/> class.
        /// </summary>
        public TempState()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TempState"/> class.
        /// </summary>
        /// <param name="properties">Properties used to describe the state of operations.</param>
        [JsonConstructor]
        public TempState(IDictionary<string, IConvertible> properties)
            : base(properties)
        {
        }

        /// <summary>
        /// A credential that can be used for temporary bootstrapping purposes.
        /// </summary>
        public string Credential
        {
            get
            {
                return this.Properties.GetValue<string>(nameof(this.Credential), true);
            }

            set
            {
                this.Properties[nameof(this.Credential)] = value;
            }
        }

        /// <summary>
        /// Returns a random credential.
        /// </summary>
        public static string GenerateCredential()
        {
            using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[16];
                generator.GetBytes(bytes);

                return Encoding.UTF8.GetString(bytes);
            }
        }
    }
}
