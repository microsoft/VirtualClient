"use strict";(self.webpackChunkvirtualclient=self.webpackChunkvirtualclient||[]).push([[1814],{3866:(e,n,s)=>{s.r(n),s.d(n,{assets:()=>a,contentTitle:()=>o,default:()=>d,frontMatter:()=>r,metadata:()=>l,toc:()=>h});var i=s(4848),t=s(8453);const r={},o="SPECpower Workload Profiles",l={id:"workloads/specpower/specpower-profiles",title:"SPECpower Workload Profiles",description:"The following profiles run customer-representative or benchmarking scenarios using the SPEC Power workload.",source:"@site/docs/workloads/specpower/specpower-profiles.md",sourceDirName:"workloads/specpower",slug:"/workloads/specpower/specpower-profiles",permalink:"/VirtualClient/docs/workloads/specpower/specpower-profiles",draft:!1,unlisted:!1,editUrl:"https://github.com/microsoft/VirtualClient/edit/main/website/docs/workloads/specpower/specpower-profiles.md",tags:[],version:"current",frontMatter:{},sidebar:"tutorialSidebar",previous:{title:"SPECpower",permalink:"/VirtualClient/docs/workloads/specpower/"},next:{title:"SPECviewperf",permalink:"/VirtualClient/docs/workloads/specview/"}},a={},h=[{value:"POWER-SPEC30.json",id:"power-spec30json",level:2},{value:"POWER-SPEC50.json",id:"power-spec50json",level:2},{value:"POWER-SPEC70.json",id:"power-spec70json",level:2},{value:"POWER-SPEC100.json",id:"power-spec100json",level:2}];function c(e){const n={a:"a",br:"br",code:"code",h1:"h1",h2:"h2",li:"li",p:"p",pre:"pre",strong:"strong",ul:"ul",...(0,t.R)(),...e.components};return(0,i.jsxs)(i.Fragment,{children:[(0,i.jsx)(n.h1,{id:"specpower-workload-profiles",children:"SPECpower Workload Profiles"}),"\n",(0,i.jsx)(n.p,{children:"The following profiles run customer-representative or benchmarking scenarios using the SPEC Power workload."}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:(0,i.jsx)(n.a,{href:"/VirtualClient/docs/workloads/specpower/",children:"Workload Details"})}),"\n"]}),"\n",(0,i.jsx)(n.h2,{id:"power-spec30json",children:"POWER-SPEC30.json"}),"\n",(0,i.jsx)(n.p,{children:"Runs the SPEC Power benchmark workload on the system targeting 30% system resource usage. This workload is an industry standard toolset for evaluating the power\nconsumption/draw on a system. Each of the different profiles is designed to use a specific percentage of the resources on the\nsystem in a steady-state usage pattern."}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsx)(n.p,{children:(0,i.jsx)(n.strong,{children:"Supported Platform/Architectures"})}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:"linux-x64"}),"\n",(0,i.jsx)(n.li,{children:"linux-arm64"}),"\n",(0,i.jsx)(n.li,{children:"win-x64"}),"\n",(0,i.jsx)(n.li,{children:"win-arm64"}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsx)(n.p,{children:(0,i.jsx)(n.strong,{children:"Supports Disconnected Scenarios"})}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:"No. Internet connection required."}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsxs)(n.p,{children:[(0,i.jsx)(n.strong,{children:"Dependencies"}),(0,i.jsx)(n.br,{}),"\n","The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively."]}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:"Internet connection."}),"\n",(0,i.jsx)(n.li,{children:"The system on which the workload is running is expected to have physical sensors on the system board in order to capture actual temperature and power readings. For example,\nAzure hosts/blades have sensors built-in to the physical hardware. When this workload runs in a virtual machine on the node/blade, the readings must be captured\non the blade itself. The CRC flighting system runs an agent on the Azure host that handles the capture of this information. The Virtual Client itself\nis used on the Azure host to capture the temperature and power metrics (using the IPMIUtil toolset)."}),"\n"]}),"\n",(0,i.jsx)(n.p,{children:"Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:"}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:(0,i.jsx)(n.a,{href:"https://microsoft.github.io/VirtualClient/docs/category/dependencies/",children:"Installing Dependencies"})}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsxs)(n.p,{children:[(0,i.jsx)(n.strong,{children:"Profile Runtimes"}),(0,i.jsx)(n.br,{}),"\n","See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile\nactions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the\nnumber of system cores."]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsxs)(n.p,{children:[(0,i.jsx)(n.strong,{children:"Usage Examples"}),(0,i.jsx)(n.br,{}),"\n","The following section provides a few basic examples of how to use the workload profile."]}),"\n",(0,i.jsx)(n.pre,{children:(0,i.jsx)(n.code,{className:"language-bash",children:'# Execute the workload profile\nVirtualClient.exe --profile=POWER-SPEC30.json --system=Juno --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"\n'})}),"\n"]}),"\n"]}),"\n",(0,i.jsx)(n.h2,{id:"power-spec50json",children:"POWER-SPEC50.json"}),"\n",(0,i.jsx)(n.p,{children:"Runs the SPEC Power benchmark workload on the system targeting 50% system resource usage. This workload is an industry standard toolset for evaluating the power\nconsumption/draw on a system. Each of the different profiles is designed to use a specific percentage of the resources on the\nsystem in a steady-state usage pattern."}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsx)(n.p,{children:(0,i.jsx)(n.strong,{children:"OS/Architecture Platforms"})}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:"linux-x64"}),"\n",(0,i.jsx)(n.li,{children:"linux-arm64"}),"\n",(0,i.jsx)(n.li,{children:"win-x64"}),"\n",(0,i.jsx)(n.li,{children:"win-arm64"}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsx)(n.p,{children:(0,i.jsx)(n.strong,{children:"Supports Disconnected Scenarios"})}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:"No. Internet connection required."}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsxs)(n.p,{children:[(0,i.jsx)(n.strong,{children:"Dependencies"}),(0,i.jsx)(n.br,{}),"\n","The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively."]}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:"Internet connection."}),"\n",(0,i.jsx)(n.li,{children:"The system on which the workload is running is expected to have physical sensors on the system board in order to capture actual temperature and power readings. For example,\nAzure hosts/blades have sensors built-in to the physical hardware. When this workload runs in a virtual machine on the node/blade, the readings must be captured\non the blade itself. The CRC flighting system runs an agent on the Azure host that handles the capture of this information. The Virtual Client itself\nis used on the Azure host to capture the temperature and power metrics (using the IPMIUtil toolset)."}),"\n"]}),"\n",(0,i.jsx)(n.p,{children:"Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:"}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:(0,i.jsx)(n.a,{href:"https://microsoft.github.io/VirtualClient/docs/category/dependencies/",children:"Installing Dependencies"})}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsxs)(n.p,{children:[(0,i.jsx)(n.strong,{children:"Profile Runtimes"}),(0,i.jsx)(n.br,{}),"\n","See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile\nactions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the\nnumber of system cores."]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsxs)(n.p,{children:[(0,i.jsx)(n.strong,{children:"Usage Examples"}),(0,i.jsx)(n.br,{}),"\n","The following section provides a few basic examples of how to use the workload profile."]}),"\n",(0,i.jsx)(n.pre,{children:(0,i.jsx)(n.code,{className:"language-bash",children:'# Execute the workload profile\nVirtualClient.exe --profile=POWER-SPEC50.json --system=Juno --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"\n'})}),"\n"]}),"\n"]}),"\n",(0,i.jsx)(n.h2,{id:"power-spec70json",children:"POWER-SPEC70.json"}),"\n",(0,i.jsx)(n.p,{children:"Runs the SPEC Power benchmark workload on the system targeting 70% system resource usage. This workload is an industry standard toolset for evaluating the power\nconsumption/draw on a system. Each of the different profiles is designed to use a specific percentage of the resources on the\nsystem in a steady-state usage pattern."}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsx)(n.p,{children:(0,i.jsx)(n.strong,{children:"OS/Architecture Platforms"})}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:"linux-x64"}),"\n",(0,i.jsx)(n.li,{children:"linux-arm64"}),"\n",(0,i.jsx)(n.li,{children:"win-x64"}),"\n",(0,i.jsx)(n.li,{children:"win-arm64"}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsx)(n.p,{children:(0,i.jsx)(n.strong,{children:"Supports Disconnected Scenarios"})}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:"No. Internet connection required."}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsxs)(n.p,{children:[(0,i.jsx)(n.strong,{children:"Dependencies"}),(0,i.jsx)(n.br,{}),"\n","The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively."]}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:"Internet connection."}),"\n",(0,i.jsx)(n.li,{children:"The system on which the workload is running is expected to have physical sensors on the system board in order to capture actual temperature and power readings. For example,\nAzure hosts/blades have sensors built-in to the physical hardware. When this workload runs in a virtual machine on the node/blade, the readings must be captured\non the blade itself. The CRC flighting system runs an agent on the Azure host that handles the capture of this information. The Virtual Client itself\nis used on the Azure host to capture the temperature and power metrics (using the IPMIUtil toolset)."}),"\n"]}),"\n",(0,i.jsx)(n.p,{children:"Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:"}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:(0,i.jsx)(n.a,{href:"https://microsoft.github.io/VirtualClient/docs/category/dependencies/",children:"Installing Dependencies"})}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsxs)(n.p,{children:[(0,i.jsx)(n.strong,{children:"Profile Runtimes"}),(0,i.jsx)(n.br,{}),"\n","See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile\nactions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the\nnumber of system cores."]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsxs)(n.p,{children:[(0,i.jsx)(n.strong,{children:"Usage Examples"}),(0,i.jsx)(n.br,{}),"\n","The following section provides a few basic examples of how to use the workload profile."]}),"\n",(0,i.jsx)(n.pre,{children:(0,i.jsx)(n.code,{className:"language-bash",children:'# Execute the workload profile\nVirtualClient.exe --profile=POWER-SPEC70.json --system=Juno --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"\n'})}),"\n"]}),"\n"]}),"\n",(0,i.jsx)(n.h2,{id:"power-spec100json",children:"POWER-SPEC100.json"}),"\n",(0,i.jsx)(n.p,{children:"Runs the SPEC Power benchmark workload on the system targeting 100% system resource usage. This workload is an industry standard toolset for evaluating the power\nconsumption/draw on a system. Each of the different profiles is designed to use a specific percentage of the resources on the\nsystem in a steady-state usage pattern."}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsx)(n.p,{children:(0,i.jsx)(n.strong,{children:"OS/Architecture Platforms"})}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:"linux-x64"}),"\n",(0,i.jsx)(n.li,{children:"linux-arm64"}),"\n",(0,i.jsx)(n.li,{children:"win-x64"}),"\n",(0,i.jsx)(n.li,{children:"win-arm64"}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsx)(n.p,{children:(0,i.jsx)(n.strong,{children:"Supports Disconnected Scenarios"})}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:"No. Internet connection required."}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsxs)(n.p,{children:[(0,i.jsx)(n.strong,{children:"Dependencies"}),(0,i.jsx)(n.br,{}),"\n","The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively."]}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:"Internet connection."}),"\n",(0,i.jsx)(n.li,{children:"The system on which the workload is running is expected to have physical sensors on the system board in order to capture actual temperature and power readings. For example,\nAzure hosts/blades have sensors built-in to the physical hardware. When this workload runs in a virtual machine on the node/blade, the readings must be captured\non the blade itself. The CRC flighting system runs an agent on the Azure host that handles the capture of this information. The Virtual Client itself\nis used on the Azure host to capture the temperature and power metrics (using the IPMIUtil toolset)."}),"\n"]}),"\n",(0,i.jsx)(n.p,{children:"Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:"}),"\n",(0,i.jsxs)(n.ul,{children:["\n",(0,i.jsx)(n.li,{children:(0,i.jsx)(n.a,{href:"https://microsoft.github.io/VirtualClient/docs/category/dependencies/",children:"Installing Dependencies"})}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsxs)(n.p,{children:[(0,i.jsx)(n.strong,{children:"Profile Runtimes"}),(0,i.jsx)(n.br,{}),"\n","See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile\nactions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the\nnumber of system cores."]}),"\n"]}),"\n",(0,i.jsxs)(n.li,{children:["\n",(0,i.jsxs)(n.p,{children:[(0,i.jsx)(n.strong,{children:"Usage Examples"}),(0,i.jsx)(n.br,{}),"\n","The following section provides a few basic examples of how to use the workload profile."]}),"\n",(0,i.jsx)(n.pre,{children:(0,i.jsx)(n.code,{className:"language-bash",children:'# Execute the workload profile\nVirtualClient.exe --profile=POWER-SPEC100.json --system=Juno --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"\n'})}),"\n"]}),"\n"]})]})}function d(e={}){const{wrapper:n}={...(0,t.R)(),...e.components};return n?(0,i.jsx)(n,{...e,children:(0,i.jsx)(c,{...e})}):c(e)}},8453:(e,n,s)=>{s.d(n,{R:()=>o,x:()=>l});var i=s(6540);const t={},r=i.createContext(t);function o(e){const n=i.useContext(r);return i.useMemo((function(){return"function"==typeof e?e(n):{...n,...e}}),[n,e])}function l(e){let n;return n=e.disableParentContext?"function"==typeof e.components?e.components(t):e.components||t:o(e.components),i.createElement(r.Provider,{value:n},e.children)}}}]);