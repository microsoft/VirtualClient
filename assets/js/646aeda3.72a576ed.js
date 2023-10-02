"use strict";(self.webpackChunkvirtualclient=self.webpackChunkvirtualclient||[]).push([[857],{3905:(e,t,r)=>{r.d(t,{Zo:()=>c,kt:()=>f});var n=r(7294);function a(e,t,r){return t in e?Object.defineProperty(e,t,{value:r,enumerable:!0,configurable:!0,writable:!0}):e[t]=r,e}function i(e,t){var r=Object.keys(e);if(Object.getOwnPropertySymbols){var n=Object.getOwnPropertySymbols(e);t&&(n=n.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),r.push.apply(r,n)}return r}function o(e){for(var t=1;t<arguments.length;t++){var r=null!=arguments[t]?arguments[t]:{};t%2?i(Object(r),!0).forEach((function(t){a(e,t,r[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(r)):i(Object(r)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(r,t))}))}return e}function l(e,t){if(null==e)return{};var r,n,a=function(e,t){if(null==e)return{};var r,n,a={},i=Object.keys(e);for(n=0;n<i.length;n++)r=i[n],t.indexOf(r)>=0||(a[r]=e[r]);return a}(e,t);if(Object.getOwnPropertySymbols){var i=Object.getOwnPropertySymbols(e);for(n=0;n<i.length;n++)r=i[n],t.indexOf(r)>=0||Object.prototype.propertyIsEnumerable.call(e,r)&&(a[r]=e[r])}return a}var p=n.createContext({}),s=function(e){var t=n.useContext(p),r=t;return e&&(r="function"==typeof e?e(t):o(o({},t),e)),r},c=function(e){var t=s(e.components);return n.createElement(p.Provider,{value:t},e.children)},u={inlineCode:"code",wrapper:function(e){var t=e.children;return n.createElement(n.Fragment,{},t)}},m=n.forwardRef((function(e,t){var r=e.components,a=e.mdxType,i=e.originalType,p=e.parentName,c=l(e,["components","mdxType","originalType","parentName"]),m=s(r),f=a,d=m["".concat(p,".").concat(f)]||m[f]||u[f]||i;return r?n.createElement(d,o(o({ref:t},c),{},{components:r})):n.createElement(d,o({ref:t},c))}));function f(e,t){var r=arguments,a=t&&t.mdxType;if("string"==typeof e||a){var i=r.length,o=new Array(i);o[0]=m;var l={};for(var p in t)hasOwnProperty.call(t,p)&&(l[p]=t[p]);l.originalType=e,l.mdxType="string"==typeof e?e:a,o[1]=l;for(var s=2;s<i;s++)o[s]=r[s];return n.createElement.apply(null,o)}return n.createElement.apply(null,r)}m.displayName="MDXCreateElement"},81:(e,t,r)=>{r.r(t),r.d(t,{assets:()=>p,contentTitle:()=>o,default:()=>u,frontMatter:()=>i,metadata:()=>l,toc:()=>s});var n=r(7462),a=(r(7294),r(3905));const i={},o="SPECviewperf Workload Profiles",l={unversionedId:"workloads/specview/specview-profiles",id:"workloads/specview/specview-profiles",title:"SPECviewperf Workload Profiles",description:"The following profile runs the SPECviewperf Workloads.",source:"@site/docs/workloads/specview/specview-profiles.md",sourceDirName:"workloads/specview",slug:"/workloads/specview/specview-profiles",permalink:"/VirtualClient/docs/workloads/specview/specview-profiles",draft:!1,editUrl:"https://github.com/microsoft/VirtualClient/edit/main/website/docs/workloads/specview/specview-profiles.md",tags:[],version:"current",frontMatter:{},sidebar:"tutorialSidebar",previous:{title:"SPECviewperf Benchmark",permalink:"/VirtualClient/docs/workloads/specview/"},next:{title:"Stress-ng",permalink:"/VirtualClient/docs/workloads/stress-ng/"}},p={},s=[{value:"PERF-GPU-SPECVIEW-AMD.json",id:"perf-gpu-specview-amdjson",level:2}],c={toc:s};function u(e){let{components:t,...r}=e;return(0,a.kt)("wrapper",(0,n.Z)({},c,r,{components:t,mdxType:"MDXLayout"}),(0,a.kt)("h1",{id:"specviewperf-workload-profiles"},"SPECviewperf Workload Profiles"),(0,a.kt)("p",null,"The following profile runs the SPECviewperf Workloads."),(0,a.kt)("ul",null,(0,a.kt)("li",{parentName:"ul"},(0,a.kt)("a",{parentName:"li",href:"/VirtualClient/docs/workloads/specview/"},"Workload Details"),"  ")),(0,a.kt)("h2",{id:"perf-gpu-specview-amdjson"},"PERF-GPU-SPECVIEW-AMD.json"),(0,a.kt)("p",null,"Runs the stock SPECviewperf Workloads."),(0,a.kt)("ul",null,(0,a.kt)("li",{parentName:"ul"},(0,a.kt)("p",{parentName:"li"},(0,a.kt)("a",{parentName:"p",href:"https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-SPECVIEW-AMD.json"},"Workload Profile")," ")),(0,a.kt)("li",{parentName:"ul"},(0,a.kt)("p",{parentName:"li"},(0,a.kt)("strong",{parentName:"p"},"Supported Platform/Architectures")),(0,a.kt)("ul",{parentName:"li"},(0,a.kt)("li",{parentName:"ul"},"win-x64"),(0,a.kt)("li",{parentName:"ul"},"AMD v620 GPU"))),(0,a.kt)("li",{parentName:"ul"},(0,a.kt)("p",{parentName:"li"},(0,a.kt)("strong",{parentName:"p"},"Supports Disconnected Scenarios"),"  "),(0,a.kt)("ul",{parentName:"li"},(0,a.kt)("li",{parentName:"ul"},"No. Internet connection required."))),(0,a.kt)("li",{parentName:"ul"},(0,a.kt)("p",{parentName:"li"},(0,a.kt)("strong",{parentName:"p"},"Dependencies"),(0,a.kt)("br",{parentName:"p"}),"\n","The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively."),(0,a.kt)("ul",{parentName:"li"},(0,a.kt)("li",{parentName:"ul"},"Internet connection.")),(0,a.kt)("p",{parentName:"li"},"Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:"),(0,a.kt)("ul",{parentName:"li"},(0,a.kt)("li",{parentName:"ul"},(0,a.kt)("a",{parentName:"li",href:"https://microsoft.github.io/VirtualClient/docs/category/dependencies/"},"Installing Dependencies")))),(0,a.kt)("li",{parentName:"ul"},(0,a.kt)("p",{parentName:"li"},(0,a.kt)("strong",{parentName:"p"},"Profile Parameters"),(0,a.kt)("br",{parentName:"p"}),"\n","NA")),(0,a.kt)("li",{parentName:"ul"},(0,a.kt)("p",{parentName:"li"},(0,a.kt)("strong",{parentName:"p"},"Profile Runtimes"),"  "),(0,a.kt)("ul",{parentName:"li"},(0,a.kt)("li",{parentName:"ul"},"The SPECviewperf package zip file is around 30GB. Downloading and extracting this file take about 30 minutes to complete. "),(0,a.kt)("li",{parentName:"ul"},"Each SPECviewperf viewset takes about 5 min to complete on a machine with a single AMD v620 GPU. Running all the viewsets takes about 40 minutes to complete."),(0,a.kt)("li",{parentName:"ul"},"The exact numbers may vary depending on the system and the internet performance. "))),(0,a.kt)("li",{parentName:"ul"},(0,a.kt)("p",{parentName:"li"},(0,a.kt)("strong",{parentName:"p"},"Usage Examples"),(0,a.kt)("br",{parentName:"p"}),"\n","The following section provides a few basic examples of how to use the workload profile."),(0,a.kt)("pre",{parentName:"li"},(0,a.kt)("code",{parentName:"pre",className:"language-bash"},'# Execute the workload profile\nVirtualClient.exe --profile=PERF-GPU-SPECVIEW-AMD.json --system=Demo --packageStore="{BlobConnectionString|SAS Uri}"\n\n# Override the profile default parameters to use a different .NET SDK version\nVirtualClient.exe --profile=PERF-GPU-SPECVIEW-AMD.json --system=Demo --packageStore="{BlobConnectionString|SAS Uri}"\n')))))}u.isMDXComponent=!0}}]);