import os
import openai
import pandas as pd
from azure.kusto.data.exceptions import KustoAuthenticationError
from azure.kusto.data.exceptions import KustoServiceError
from azure.kusto.data.helpers import dataframe_from_result_table
from azure.kusto.data import KustoClient, KustoConnectionStringBuilder
import adal

openai.api_type = "azure"
openai.api_base = "https://vc-openai.openai.azure.com/"
openai.api_version = "2023-03-15-preview"
openai.api_key = os.getenv("vc-openai-key")

cluster = "<YOUR_KUSTO_CLUSTER_HERE>"
client_id = "<YOUR_AAD_CLIENT_ID_HERE>"
client_secret = "<YOUR_AAD_CLIENT_SECRET_HERE>"
tenant_id = "<YOUR_AAD_TENANT_ID_HERE>"
authority_uri = f"https://login.microsoftonline.com/{tenant_id}"

# Define a function to authenticate with AAD and create a Kusto client
def authenticate_with_aad_and_create_client():
    context = adal.AuthenticationContext(authority_uri)
    token = context.acquire_token_with_client_credentials(
        resource=cluster,
        client_id=client_id,
        client_secret=client_secret
    )
    connection_string = KustoConnectionStringBuilder.with_aad_application_token_authentication(
        f"https://{cluster}.kusto.windows.net",
        token["accessToken"]
    )
    return KustoClient(connection_string)


with open('src/openai/prompt2.txt', 'r') as file:
    prompt = file.read()

conversation = [
              {
                "role": "system",
                "content": prompt
              }
            ]

while(True):
    user_input = input()      
    conversation.append({"role": "user", "content": user_input})
    response = openai.ChatCompletion.create(
      engine="chat",
      messages = conversation,
      temperature=1,
      max_tokens=2000,
      top_p=0.95,
      frequency_penalty=0,
      presence_penalty=0,
      stop=None)

    conversation.append({"role": "assistant", "content": response['choices'][0]['message']['content']})
    print("\n" + response['choices'][0]['message']['content'] + "\n")
    print("------------------------------------------")



