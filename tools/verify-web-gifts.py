#!/usr/bin/env python3
"""Compile HOST against real Mif/net472, run local simulated tests on .NET8.
No game/server access. State DLL is unchanged and not built by this runner.
"""
import argparse,json,os,shutil,subprocess
from pathlib import Path
p=argparse.ArgumentParser()
for n in ['dotnet','compiler','framework','host-mif','protobuf','output']:p.add_argument('--'+n,type=Path,required=True)
a=p.parse_args();root=Path(__file__).resolve().parents[1];out=a.output.resolve();out.mkdir(parents=True,exist_ok=True)
refs=[a.framework.resolve()/n for n in ['mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll']]+[a.host_mif.resolve()]
for f in refs+[a.protobuf]:
 if not f.is_file():raise SystemExit('Missing real dependency: '+str(f))
shutil.copyfile(a.host_mif,out/'Mif.dll');shutil.copyfile(a.protobuf,out/'protobuf-net.dll');shutil.copytree(root/'localization',out/'Languages',dirs_exist_ok=True)
base=[str(a.dotnet),str(a.compiler),'-nologo','-noconfig','-nostdlib+','-deterministic+','-optimize+']+['-r:'+str(f) for f in refs]+['-resource:'+str(root/'localization/en.xml')+',QH.en.xml']
host=[str(f) for f in sorted((root/'src/QuantumHangarQc').glob('*.cs')) if f.name!='Tests.cs']+[str(root/'src/Shared/Localization.cs')]
def run(name,cmd):
 r=subprocess.run(cmd,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,env={**os.environ,'QH_TEST_DOTNET':str(a.dotnet)})
 (out/(name+'.log')).write_text(r.stdout);print(name,r.returncode,r.stdout[-1200:]);r.check_returncode()
run('build-host',base+['-target:library','-out:'+str(out/'QuantumHangarQc.dll')]+host)
for name,extra,args in [('Tests',[root/'src/QuantumHangarQc/Tests.cs'],[out/'host-worlds']),('WebGiftTests',[root/'tests/WebGiftTests.cs'],[out/'gift-worlds']),('RestoreTests',[root/'src/QuantumHangarQc/Tests.cs',root/'tests/RestoreTests.cs'],[out/'restore-worlds'])]:
 exe=out/(name+'.exe');run('build-'+name,base+['-target:exe','-main:'+name,'-out:'+str(exe)]+host+[str(f) for f in extra]);exe.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}));run(name,[str(a.dotnet),str(exe)]+list(map(str,args)))
print('Local tests PASS. Windows/Empyrion/real cargo: NOT_TESTED.')
