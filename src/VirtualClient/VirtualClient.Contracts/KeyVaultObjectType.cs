namespace VirtualClient.Contracts
{
    /// <summary>
    /// Enumerates the types of objects stored in Azure Key Vault.
    /// </summary>
    public enum KeyVaultObjectType
    {
        /// <summary>
        /// Secret type KeyVault Object
        /// </summary>
        Secret,

        /// <summary>
        /// Key type KeyVault Object
        /// </summary>
        Key,

        /// <summary>
        /// Certificate type KeyVault Object
        /// </summary>
        Certificate
    }
}
