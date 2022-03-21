import{r as n,o as a,c as s,a as e,e as r,F as l,b as o,d as i}from"./app.749a123f.js";import{_ as c}from"./plugin-vue_export-helper.21dcd24c.js";const d={},u=e("h2",{id:"installing",tabindex:"-1"},[e("a",{class:"header-anchor",href:"#installing","aria-hidden":"true"},"#"),o(" Installing")],-1),h=o("Just install the required packages into your project with "),_=e("code",null,"dotnet add package",-1),p=o(". You can find the packages available "),m={href:"https://www.nuget.org/packages?q=censorcore",target:"_blank",rel:"noopener noreferrer"},b=o("on the NuGet Gallery"),f=o(". Most of the time, you will want at least "),g=e("code",null,"CensorCore",-1),k=o(" and "),y=e("code",null,"Censor.ModelLoader",-1),C=o(", but if you want to include the REST API, you may also want "),w=e("code",null,"CensorCore.Web",-1),v=o("."),x=i(`<blockquote><p>Check out the source for <code>CensorCore.Console</code> to see a rough implementation of what a CensorCore consumer looks like.</p></blockquote><h2 id="build" tabindex="-1"><a class="header-anchor" href="#build" aria-hidden="true">#</a> Build</h2><p>If you&#39;re feeling adventurous and want to build this for yourself, make sure you have the .NET 6 SDK installed and ready, then just run the following:</p><div class="language-bash ext-sh line-numbers-mode"><pre class="language-bash"><code>dotnet tool restore
dotnet cake
</code></pre><div class="line-numbers" aria-hidden="true"><span class="line-number">1</span><br><span class="line-number">2</span><br></div></div><p>Note that the <code>Publish</code> target is the more &quot;complete&quot; build task, but may require you to include the correct model file in the repository to build the CLI.</p>`,5);function q(N,I){const t=n("ExternalLinkIcon");return a(),s(l,null,[u,e("p",null,[h,_,p,e("a",m,[b,r(t)]),f,g,k,y,C,w,v]),x],64)}var L=c(d,[["render",q]]);export{L as default};