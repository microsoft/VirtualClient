import os
import openai
openai.api_type = "azure"
openai.api_base = "https://vc-openai.openai.azure.com/"
openai.api_version = "2023-03-15-preview"
openai.api_key = os.getenv("vc-openai-key")

with open('src/openai/vc-chatbot-prompt.txt', 'r') as file:
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
      max_tokens=400,
      top_p=0.95,
      frequency_penalty=0,
      presence_penalty=0,
      stop=None)

    conversation.append({"role": "assistant", "content": response['choices'][0]['message']['content']})
    print("\n" + response['choices'][0]['message']['content'] + "\n")
    print("------------------------------------------")



''''
workloads=['aspnetbench','compression','coremark','dcgmi','deathstarbench','diskspd','fio','geekbench',
           'graph500','hpcg','lapack','lmbench','memcached','mlperf','nasparallel','network-suite','openfoam',
           'openssl','postgresql','prime95','redis','speccpu','specjbb','specjvm','specpower','stress-ng','stressapptest',
           'superbenchmark','sysbench-oltp']


for workload in workloads:
    with open(f'website/docs/workloads/{workload}/{workload}.md', 'r', encoding="utf8") as file:
      doc = file.read()
    message ={ 'role': "system", 'content': f"This is the information for workload {workload}: {doc}"}
    messages.append(message)
'''