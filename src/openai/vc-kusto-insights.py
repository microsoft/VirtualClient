import os
import re
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

cluster = "https://azurecrc.westus2.kusto.windows.net"
client_id = "d8509cac-d9a0-4676-b5ad-9926c3f1bcb1"
client_secret = os.getenv("crc-kusto-key")
tenant_id = "33e01921-4d64-4f8c-a055-5bdaffd5e33d"
authority_uri = f"https://login.microsoftonline.com/{tenant_id}"


# Define a function to run a Kusto query and return the results
def run_kusto_query(query):
    try:
        client = authenticate_with_aad_and_create_client()
        response = client.execute("WorkloadPerformance", query)
        results = dataframe_from_result_table(response.primary_results[0])
        return results
    except KustoAuthenticationError as ex:
        print("Kusto authentication failed: " + str(ex))
    except KustoServiceError as ex:
        print("Kusto request had failed: " + str(ex))

# Define a function to authenticate with AAD and create a Kusto client
def authenticate_with_aad_and_create_client():
    context = adal.AuthenticationContext(authority_uri)
    token = context.acquire_token_with_client_credentials(
        resource=cluster,
        client_id=client_id,
        client_secret=client_secret
    )
    connection_string = KustoConnectionStringBuilder.with_aad_application_token_authentication(
        f"https://azurecrcworkloads.westus2.kusto.windows.net/WorkloadPerformance",
        token["accessToken"]
    )
    return KustoClient(connection_string)


with open('src/openai/vc-kusto-insights-prompt.txt', 'r') as file:
    prompt = file.read()

conversation = [
              {
                "role": "system",
                "content": prompt
              }
            ]


user_input = input()      
conversation.append({"role": "user", "content": user_input})
response = openai.ChatCompletion.create(
  engine="chat",
  messages = conversation,
  temperature=0.5,
  max_tokens=2000,
  top_p=0.95,
  frequency_penalty=0,
  presence_penalty=0,
  stop=None)

conversation.append({"role": "assistant", "content": response['choices'][0]['message']['content']})
print("\n" + response['choices'][0]['message']['content'] + "\n")
print("------------------------------------------")

kusto_query = response.choices[0]['message']['content']

# Define the regular expression pattern to match text between triple backticks
pattern = r"```(.*?)```"

# Use the re.findall method to extract all matches of the pattern
matches = re.findall(pattern, kusto_query, re.DOTALL)

results = run_kusto_query(matches[0])

conversation.append({"role": "assistant", "content": f"What insight can you get from this data: {results.to_string(index=False)}"})
response = openai.ChatCompletion.create(
  engine="chat",
  messages = conversation,
  temperature=0.5,
  max_tokens=2000,
  top_p=0.95,
  frequency_penalty=0,
  presence_penalty=0,
  stop=None)
print("\n" + response['choices'][0]['message']['content'] + "\n")
print("------------------------------------------")


