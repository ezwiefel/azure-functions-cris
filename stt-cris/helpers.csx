#r "Microsoft.WindowsAzure.Storage"

using System;
using Microsoft.WindowsAzure.Storage;


///<summary>
/// Syntactic sugar to retrieve environment variable
///</summary>
public static string GetEnvironmentVariable(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}

///<summary>
///Lightweight wrapper to return connection string for AzureStorageAccount
///</summary>
public abstract class AzureStorageAccount
{
    public static string ConnectionString = $"DefaultEndpointsProtocol=https;AccountName={GetEnvironmentVariable("storage_account_name")};AccountKey={GetEnvironmentVariable("storage_account_key")}";
}