"use strict";(self.webpackChunkvirtualclient=self.webpackChunkvirtualclient||[]).push([[2562],{6432:(e,r,n)=>{n.r(r),n.d(r,{assets:()=>c,contentTitle:()=>t,default:()=>p,frontMatter:()=>i,metadata:()=>l,toc:()=>s});var a=n(5893),o=n(3905);const i={},t="Run Commercial Workloads: Bring Your Own Package",l={id:"guides/0030-commercial-workloads",title:"Run Commercial Workloads: Bring Your Own Package",description:'Virtual Client supports running commercial workloads. However, we can not distribute the binary and licenses for the commercial workloads. In those cases, users need to "bring their own binary and license".',source:"@site/docs/guides/0030-commercial-workloads.md",sourceDirName:"guides",slug:"/guides/0030-commercial-workloads",permalink:"/VirtualClient/docs/guides/0030-commercial-workloads",draft:!1,unlisted:!1,editUrl:"https://github.com/microsoft/VirtualClient/edit/main/website/docs/guides/0030-commercial-workloads.md",tags:[],version:"current",frontMatter:{},sidebar:"tutorialSidebar",previous:{title:"Client/Server Support",permalink:"/VirtualClient/docs/guides/0020-client-server"},next:{title:"Data/Telemetry Support",permalink:"/VirtualClient/docs/guides/0040-telemetry"}},c={},s=[{value:"Supported commercial workloads",id:"supported-commercial-workloads",level:2},{value:"Supporting Commercial Workloads",id:"supporting-commercial-workloads",level:2},{value:"Step 1: VirtualClient Downloads Your Package",id:"step-1-virtualclient-downloads-your-package",level:2},{value:".vcpkg file",id:"vcpkg-file",level:3},{value:"Prepare your package",id:"prepare-your-package",level:3},{value:"Upload your package to some storage",id:"upload-your-package-to-some-storage",level:3},{value:"Add package download to your VC profile",id:"add-package-download-to-your-vc-profile",level:3},{value:"Let VirtualClient discover your local packages",id:"let-virtualclient-discover-your-local-packages",level:2},{value:"Put packages in the packages directory with .vcpkg file.",id:"put-packages-in-the-packages-directory-with-vcpkg-file",level:3},{value:"Define alternative package directory using environment variable",id:"define-alternative-package-directory-using-environment-variable",level:3}];function d(e){const r={a:"a",admonition:"admonition",code:"code",em:"em",h1:"h1",h2:"h2",h3:"h3",li:"li",p:"p",pre:"pre",strong:"strong",ul:"ul",...(0,o.ah)(),...e.components};return(0,a.jsxs)(a.Fragment,{children:[(0,a.jsx)(r.h1,{id:"run-commercial-workloads-bring-your-own-package",children:"Run Commercial Workloads: Bring Your Own Package"}),"\n",(0,a.jsx)(r.p,{children:'Virtual Client supports running commercial workloads. However, we can not distribute the binary and licenses for the commercial workloads. In those cases, users need to "bring their own binary and license".'}),"\n",(0,a.jsx)(r.admonition,{type:"warning",children:(0,a.jsx)(r.p,{children:(0,a.jsx)(r.em,{children:"The Virtual Client team is currently working to define and document the process for integration of commercial workloads into the Virtual Client.\nThe contents of this document are NOT complete and are meant only to illustrate the basic concepts. Please bear with us while we are figuring this\nprocess out."})})}),"\n",(0,a.jsx)(r.h2,{id:"supported-commercial-workloads",children:"Supported commercial workloads"}),"\n",(0,a.jsx)(r.p,{children:"The following workloads are commercial (requiring purchase and/or license) software supported by the Virtual Client."}),"\n",(0,a.jsxs)(r.ul,{children:["\n",(0,a.jsx)(r.li,{children:(0,a.jsx)(r.strong,{children:(0,a.jsx)(r.a,{href:"/VirtualClient/docs/workloads/geekbench/",children:"Geekbench"})})}),"\n",(0,a.jsx)(r.li,{children:(0,a.jsx)(r.strong,{children:(0,a.jsx)(r.a,{href:"/VirtualClient/docs/workloads/speccpu/",children:"SPECcpu"})})}),"\n",(0,a.jsx)(r.li,{children:(0,a.jsx)(r.strong,{children:(0,a.jsx)(r.a,{href:"/VirtualClient/docs/workloads/specjbb/",children:"SPECjbb"})})}),"\n",(0,a.jsx)(r.li,{children:(0,a.jsx)(r.strong,{children:(0,a.jsx)(r.a,{href:"/VirtualClient/docs/workloads/specpower/",children:"SPECpower"})})}),"\n"]}),"\n",(0,a.jsx)(r.h2,{id:"supporting-commercial-workloads",children:"Supporting Commercial Workloads"}),"\n",(0,a.jsx)(r.p,{children:"The following sections describe how the process of integrating commercial workloads into the Virtual Client works. With commercial workloads, the user must\nhave purchased the software or the license and have created a VC package for integration with the runtime platform. The following steps describe how this typically\ncomes together."}),"\n",(0,a.jsx)(r.h2,{id:"step-1-virtualclient-downloads-your-package",children:"Step 1: VirtualClient Downloads Your Package"}),"\n",(0,a.jsx)(r.h3,{id:"vcpkg-file",children:".vcpkg file"}),"\n",(0,a.jsxs)(r.p,{children:[".vcpkg is just a json file in vcpkg extension, that VC uses to register package information. While in many cases this file is optional, it is\nhighly recommended to make a .vcpkg file when preparing your own package. The file has one required property ",(0,a.jsx)(r.code,{children:"name"}),", and optional properties"]}),"\n",(0,a.jsx)(r.p,{children:"This example vcpkg file"}),"\n",(0,a.jsx)(r.pre,{children:(0,a.jsx)(r.code,{className:"language-json",children:'{\n    "name": "lshw",\n    "description": "Hardware lister for Linux toolset.",\n    "version": "B.02.19.59",\n    "metadata": {\n        "commit": "https://github.com/lyonel/lshw/commit/996aaad9c760efa6b6ffef8518999ec226af049a",\n        "tags": "pre-release"\n    }\n}\n'})}),"\n",(0,a.jsx)(r.h3,{id:"prepare-your-package",children:"Prepare your package"}),"\n",(0,a.jsxs)(r.ul,{children:["\n",(0,a.jsx)(r.li,{children:"There are generally two types of packages: Post-compile, and others (pre-compile/no-compile-needed)."}),"\n",(0,a.jsx)(r.li,{children:"Post-compile packages are generally OS or architecture specific, they need to be in their"}),"\n",(0,a.jsx)(r.li,{children:"The package structure could be workload-specific, refer to the workload documentation to see the packaging instructions."}),"\n"]}),"\n",(0,a.jsx)(r.h3,{id:"upload-your-package-to-some-storage",children:"Upload your package to some storage"}),"\n",(0,a.jsxs)(r.ul,{children:["\n",(0,a.jsx)(r.li,{children:"Right now VC only supports Azure Storage Account"}),"\n",(0,a.jsxs)(r.li,{children:["We are open to support other major storage services. ",(0,a.jsx)(r.strong,{children:"Contributions welcomed!"})]}),"\n"]}),"\n",(0,a.jsx)(r.h3,{id:"add-package-download-to-your-vc-profile",children:"Add package download to your VC profile"}),"\n",(0,a.jsx)(r.pre,{children:(0,a.jsx)(r.code,{className:"language-json",children:'{\n  "Type": "DependencyPackageInstallation",\n  "Parameters": {\n    "Scenario": "InstallSPECcpuWorkloadPackage",\n    "BlobContainer": "packages",\n    "BlobName": "speccpu.2017.1.1.8.zip",\n    "PackageName": "speccpu2017",\n    "Extract": true\n  }\n}\n'})}),"\n",(0,a.jsx)(r.h2,{id:"let-virtualclient-discover-your-local-packages",children:"Let VirtualClient discover your local packages"}),"\n",(0,a.jsx)(r.h3,{id:"put-packages-in-the-packages-directory-with-vcpkg-file",children:"Put packages in the packages directory with .vcpkg file."}),"\n",(0,a.jsxs)(r.p,{children:["User also have the option to put in workload packages under ",(0,a.jsx)(r.code,{children:"virtualclient/packages"})," directory. Any directory with a ",(0,a.jsx)(r.code,{children:".vcpkg"})," inside will be registered as a VC package."]}),"\n",(0,a.jsx)(r.h3,{id:"define-alternative-package-directory-using-environment-variable",children:"Define alternative package directory using environment variable"}),"\n",(0,a.jsxs)(r.p,{children:["A user of the Virtual Client can define an environment variable called ",(0,a.jsx)(r.strong,{children:"VCDependenciesPath"}),". This directory will be used\nto discover packages with the highest priority. If a package is not defined here, the Virtual Client will look for the package in the\nlocations noted below. If a package is found in this location, Virtual Client will not search for other locations. The package found\nhere will be used."]}),"\n",(0,a.jsx)(r.p,{children:"Package parent directory names should ALWAYS be lower-cased (e.g. geekbench5 vs. Geekbench5)."}),"\n",(0,a.jsx)(r.pre,{children:(0,a.jsx)(r.code,{children:"e.g.\nset VCDependenciesPath=C:\\any\\custom\\packages\\location\n\nC:\\any\\custom\\packages\\location\\geekbench5\nC:\\any\\custom\\packages\\location\\geekbench5\\linux-x64\nC:\\any\\custom\\packages\\location\\geekbench5\\linux-arm64\nC:\\any\\custom\\packages\\location\\geekbench5\\win-x64\nC:\\any\\custom\\packages\\location\\geekbench5\\win-arm64\n"})})]})}function p(e={}){const{wrapper:r}={...(0,o.ah)(),...e.components};return r?(0,a.jsx)(r,{...e,children:(0,a.jsx)(d,{...e})}):d(e)}},3905:(e,r,n)=>{n.d(r,{ah:()=>s});var a=n(7294);function o(e,r,n){return r in e?Object.defineProperty(e,r,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[r]=n,e}function i(e,r){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var a=Object.getOwnPropertySymbols(e);r&&(a=a.filter((function(r){return Object.getOwnPropertyDescriptor(e,r).enumerable}))),n.push.apply(n,a)}return n}function t(e){for(var r=1;r<arguments.length;r++){var n=null!=arguments[r]?arguments[r]:{};r%2?i(Object(n),!0).forEach((function(r){o(e,r,n[r])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):i(Object(n)).forEach((function(r){Object.defineProperty(e,r,Object.getOwnPropertyDescriptor(n,r))}))}return e}function l(e,r){if(null==e)return{};var n,a,o=function(e,r){if(null==e)return{};var n,a,o={},i=Object.keys(e);for(a=0;a<i.length;a++)n=i[a],r.indexOf(n)>=0||(o[n]=e[n]);return o}(e,r);if(Object.getOwnPropertySymbols){var i=Object.getOwnPropertySymbols(e);for(a=0;a<i.length;a++)n=i[a],r.indexOf(n)>=0||Object.prototype.propertyIsEnumerable.call(e,n)&&(o[n]=e[n])}return o}var c=a.createContext({}),s=function(e){var r=a.useContext(c),n=r;return e&&(n="function"==typeof e?e(r):t(t({},r),e)),n},d={inlineCode:"code",wrapper:function(e){var r=e.children;return a.createElement(a.Fragment,{},r)}},p=a.forwardRef((function(e,r){var n=e.components,o=e.mdxType,i=e.originalType,c=e.parentName,p=l(e,["components","mdxType","originalType","parentName"]),u=s(n),h=o,g=u["".concat(c,".").concat(h)]||u[h]||d[h]||i;return n?a.createElement(g,t(t({ref:r},p),{},{components:n})):a.createElement(g,t({ref:r},p))}));p.displayName="MDXCreateElement"}}]);