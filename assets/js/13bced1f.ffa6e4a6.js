"use strict";(self.webpackChunkvirtualclient=self.webpackChunkvirtualclient||[]).push([[8728],{6578:(e,n,t)=>{t.r(n),t.d(n,{assets:()=>a,contentTitle:()=>l,default:()=>c,frontMatter:()=>i,metadata:()=>o,toc:()=>h});var s=t(4848),r=t(8453);const i={},l="NAS Parallel Workload Profiles",o={id:"workloads/nasparallel/nasparallel-profiles",title:"NAS Parallel Workload Profiles",description:"The following profiles run customer-representative or benchmarking scenarios using the NAS Parallel toolset.",source:"@site/docs/workloads/nasparallel/nasparallel-profiles.md",sourceDirName:"workloads/nasparallel",slug:"/workloads/nasparallel/nasparallel-profiles",permalink:"/VirtualClient/docs/workloads/nasparallel/nasparallel-profiles",draft:!1,unlisted:!1,editUrl:"https://github.com/microsoft/VirtualClient/edit/main/website/docs/workloads/nasparallel/nasparallel-profiles.md",tags:[],version:"current",frontMatter:{},sidebar:"tutorialSidebar",previous:{title:"NAS Parallel",permalink:"/VirtualClient/docs/workloads/nasparallel/"},next:{title:"Network Ping/ICMP",permalink:"/VirtualClient/docs/workloads/network-ping/"}},a={},h=[{value:"Client/Server Topology Support",id:"clientserver-topology-support",level:2},{value:"SSH Requirements",id:"ssh-requirements",level:2},{value:"PERF-HPC-NASPARALLELBENCH.json",id:"perf-hpc-nasparallelbenchjson",level:2}];function d(e){const n={a:"a",br:"br",code:"code",h1:"h1",h2:"h2",li:"li",p:"p",pre:"pre",strong:"strong",table:"table",tbody:"tbody",td:"td",th:"th",thead:"thead",tr:"tr",ul:"ul",...(0,r.R)(),...e.components};return(0,s.jsxs)(s.Fragment,{children:[(0,s.jsx)(n.h1,{id:"nas-parallel-workload-profiles",children:"NAS Parallel Workload Profiles"}),"\n",(0,s.jsx)(n.p,{children:"The following profiles run customer-representative or benchmarking scenarios using the NAS Parallel toolset."}),"\n",(0,s.jsxs)(n.ul,{children:["\n",(0,s.jsx)(n.li,{children:(0,s.jsx)(n.a,{href:"/VirtualClient/docs/workloads/nasparallel/",children:"Workload Details"})}),"\n",(0,s.jsx)(n.li,{children:(0,s.jsx)(n.a,{href:"/VirtualClient/docs/guides/0020-client-server",children:"Client/Server Workloads"})}),"\n"]}),"\n",(0,s.jsx)(n.h2,{id:"clientserver-topology-support",children:"Client/Server Topology Support"}),"\n",(0,s.jsx)(n.p,{children:"NAS Parallel workload profiles support running the workload on both a single system as well as in an multi-system, client/server topology. This means that the workload supports\noperation on a single system or on N number of distinct systems. The client/server topology is typically used when it is desirable to include a network component in the\noverall performance evaluation. In a client/server topology, one system operates in the 'Client' role making calls to the system operating in the 'Server' role.\nThe Virtual Client instances running on the client and server systems will synchronize with each other before running the workload. In order to support a client/server topology,\nan environment layout file MUST be supplied to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances. An\nenvironment layout file is not required for the single system topology."}),"\n",(0,s.jsx)(n.p,{children:"The Virtual Client running on the client and server systems will synchronize with each other before running each individual workload. An environment layout\nfile MUST be supplied to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances."}),"\n",(0,s.jsx)(n.p,{children:(0,s.jsx)(n.a,{href:"/VirtualClient/docs/guides/0020-client-server",children:"Environment Layouts"})}),"\n",(0,s.jsx)(n.p,{children:'In the environment layout file provided to the Virtual Client, define the role of the client system/VM as "Client" and the role of the server system(s)/VM(s) as "Server".\nThe spelling of the roles must be exact. The IP addresses of the systems/VMs must be correct as well. The following example illustrates the\nidea. The name of the client must match the name of the system or the value of the agent ID passed in on the command line.'}),"\n",(0,s.jsx)(n.p,{children:"For different benchmarks with NAS Parallel we have various recommendation on number of nodes as mentioned below."}),"\n",(0,s.jsxs)(n.ul,{children:["\n",(0,s.jsxs)(n.li,{children:["\n",(0,s.jsxs)(n.p,{children:["BT, SP benchmarks",(0,s.jsx)(n.br,{}),"\n","A square number of processes (1, 4, 9, ...)."]}),"\n"]}),"\n",(0,s.jsxs)(n.li,{children:["\n",(0,s.jsxs)(n.p,{children:["LU benchmark",(0,s.jsx)(n.br,{}),"\n","2D (n1 * n2) process grid where  n1/2 ","<="," n2 ","<="," n1 ."]}),"\n"]}),"\n",(0,s.jsxs)(n.li,{children:["\n",(0,s.jsxs)(n.p,{children:["CG, FT, IS, MG benchmarks",(0,s.jsx)(n.br,{}),"\n","a power-of-two number of processes (1, 2, 4, ...)."]}),"\n"]}),"\n",(0,s.jsxs)(n.li,{children:["\n",(0,s.jsxs)(n.p,{children:["EP benchmark",(0,s.jsx)(n.br,{}),"\n","No special requirements."]}),"\n"]}),"\n",(0,s.jsxs)(n.li,{children:["\n",(0,s.jsxs)(n.p,{children:["DC,UA benchmarks",(0,s.jsx)(n.br,{}),"\n","Run only on single machine."]}),"\n"]}),"\n",(0,s.jsxs)(n.li,{children:["\n",(0,s.jsxs)(n.p,{children:["DT benchmark",(0,s.jsx)(n.br,{}),"\n","Minimum of 5 machines required."]}),"\n"]}),"\n"]}),"\n",(0,s.jsx)(n.pre,{children:(0,s.jsx)(n.code,{className:"language-bash",children:'# Single System (environment layout not required)\n./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440\n\n# Multi-System\n# On the Client role system (the controller)\n./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Client01 --layoutPath=/any/path/to/layout.json\n\n# On Server role system #1\n./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Server01 --layoutPath=/any/path/to/layout.json\n\n# On Server role system #2\n./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Server02 --layoutPath=/any/path/to/layout.json\n\n# Example contents of the \'layout.json\' file:\n{\n    "clients": [\n        {\n            "name": "Client01",\n            "role": "Client",\n            "privateIPAddress": "10.1.0.1"\n        },\n        {\n            "name": "Server01",\n            "role": "Server",\n            "privateIPAddress": "10.1.0.2"\n        },\n        {\n            "name": "Server02",\n            "role": "Server",\n            "privateIPAddress": "10.1.0.3"\n        }\n    ]\n}\n'})}),"\n",(0,s.jsx)(n.h2,{id:"ssh-requirements",children:"SSH Requirements"}),"\n",(0,s.jsxs)(n.p,{children:["OpenMPI sends messages over port 22 - as well as expects to send messages without having to supply a key or passsword. A secure and safe way is to register an SSH identity with the\nclient machine. Here is an example ",(0,s.jsx)(n.a,{href:"https://linuxize.com/post/how-to-setup-passwordless-ssh-login/",children:"blog post"})," on how to do this. Although the basic steps are:"]}),"\n",(0,s.jsxs)(n.ul,{children:["\n",(0,s.jsx)(n.li,{children:"On client, store a private-public key pair under ~/.ssh/id_rsa and ~/.ssh/id_rsa.pub"}),"\n",(0,s.jsx)(n.li,{children:"On server, append the id_rsa.pub generated under ~/.ssh/authorized_keys"}),"\n",(0,s.jsx)(n.li,{children:"On client, store server fingprints in ~/.ssh/known_hosts"}),"\n",(0,s.jsx)(n.li,{children:"Last when running the profile, supply the username whos .ssh directory contains all of the files just created/edited."}),"\n"]}),"\n",(0,s.jsx)(n.h2,{id:"perf-hpc-nasparallelbenchjson",children:"PERF-HPC-NASPARALLELBENCH.json"}),"\n",(0,s.jsx)(n.p,{children:"Runs a set of HPC workloads using NAS Parallel Benchmarks to the parallel computing performance. This profile is designed to test both single and\nmultiple nodes performance."}),"\n",(0,s.jsxs)(n.ul,{children:["\n",(0,s.jsxs)(n.li,{children:["\n",(0,s.jsx)(n.p,{children:(0,s.jsx)(n.a,{href:"https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-HPC-NASPARALLELBENCH.json",children:"Workload Profile"})}),"\n"]}),"\n",(0,s.jsxs)(n.li,{children:["\n",(0,s.jsx)(n.p,{children:(0,s.jsx)(n.strong,{children:"Supported Platform/Architectures"})}),"\n",(0,s.jsxs)(n.ul,{children:["\n",(0,s.jsx)(n.li,{children:"linux-x64"}),"\n",(0,s.jsx)(n.li,{children:"linux-arm64"}),"\n"]}),"\n"]}),"\n",(0,s.jsxs)(n.li,{children:["\n",(0,s.jsx)(n.p,{children:(0,s.jsx)(n.strong,{children:"Supports Disconnected Scenarios"})}),"\n",(0,s.jsxs)(n.ul,{children:["\n",(0,s.jsx)(n.li,{children:"No. Internet connection required."}),"\n"]}),"\n"]}),"\n",(0,s.jsxs)(n.li,{children:["\n",(0,s.jsxs)(n.p,{children:[(0,s.jsx)(n.strong,{children:"Dependencies"}),(0,s.jsx)(n.br,{}),"\n","The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively."]}),"\n",(0,s.jsxs)(n.ul,{children:["\n",(0,s.jsx)(n.li,{children:"Internet connection."}),"\n",(0,s.jsx)(n.li,{children:"For multi-system scenarios, communications over SSH port 22 must be allowed."}),"\n",(0,s.jsx)(n.li,{children:"The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct."}),"\n",(0,s.jsx)(n.li,{children:"The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --agentId)\nor must match the name of the system as defined by the operating system itself."}),"\n"]}),"\n",(0,s.jsx)(n.p,{children:"Additional information on individual components that exist within the 'Dependencies' section of the profile can be found in the following locations:"}),"\n",(0,s.jsxs)(n.ul,{children:["\n",(0,s.jsxs)(n.li,{children:[(0,s.jsx)(n.a,{href:"https://microsoft.github.io/VirtualClient/docs/category/dependencies/",children:"Installing Dependencies"}),"."]}),"\n"]}),"\n"]}),"\n",(0,s.jsxs)(n.li,{children:["\n",(0,s.jsxs)(n.p,{children:[(0,s.jsx)(n.strong,{children:"Profile Parameters"}),(0,s.jsx)(n.br,{}),"\n","The following parameters can be optionally supplied on the command line to modify the behaviors of the workload."]}),"\n",(0,s.jsxs)(n.table,{children:[(0,s.jsx)(n.thead,{children:(0,s.jsxs)(n.tr,{children:[(0,s.jsx)(n.th,{children:"Parameter"}),(0,s.jsx)(n.th,{children:"Purpose"}),(0,s.jsx)(n.th,{children:"Default Value"})]})}),(0,s.jsx)(n.tbody,{children:(0,s.jsxs)(n.tr,{children:[(0,s.jsx)(n.td,{children:"Username"}),(0,s.jsx)(n.td,{children:"Required. See 'SSH Requirements' above"}),(0,s.jsx)(n.td,{children:"No default, must be supplied"})]})})]}),"\n"]}),"\n",(0,s.jsxs)(n.li,{children:["\n",(0,s.jsxs)(n.p,{children:[(0,s.jsx)(n.strong,{children:"Profile Runtimes"}),(0,s.jsx)(n.br,{}),"\n","See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile\nactions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the\nnumber of system cores."]}),"\n"]}),"\n",(0,s.jsxs)(n.li,{children:["\n",(0,s.jsxs)(n.p,{children:[(0,s.jsx)(n.strong,{children:"Usage Examples"}),(0,s.jsx)(n.br,{}),"\n","The following section provides a few basic examples of how to use the workload profile."]}),"\n",(0,s.jsx)(n.pre,{children:(0,s.jsx)(n.code,{className:"language-bash",children:'# When running on a single system (environment layout not required)\n./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --parameters="Username=testuser" --packageStore="{BlobConnectionString|SAS Uri}"\n\n # When running in a client/server environment\n./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Client01 --parameters="Username=testuser" --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"\n./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Server01 --parameters="Username=testuser" --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"\n\n# When running in a client/server environment with additional systems\n./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Client01 --parameters="Username=testuser" --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"\n./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Server01 --parameters="Username=testuser" --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"\n./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Server02 --parameters="Username=testuser" --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"\n'})}),"\n"]}),"\n"]})]})}function c(e={}){const{wrapper:n}={...(0,r.R)(),...e.components};return n?(0,s.jsx)(n,{...e,children:(0,s.jsx)(d,{...e})}):d(e)}},8453:(e,n,t)=>{t.d(n,{R:()=>l,x:()=>o});var s=t(6540);const r={},i=s.createContext(r);function l(e){const n=s.useContext(i);return s.useMemo((function(){return"function"==typeof e?e(n):{...n,...e}}),[n,e])}function o(e){let n;return n=e.disableParentContext?"function"==typeof e.components?e.components(r):e.components||r:l(e.components),s.createElement(i.Provider,{value:n},e.children)}}}]);