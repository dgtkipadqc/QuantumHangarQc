#!/usr/bin/env python3
"""Compile with Microsoft net472 reference assemblies and real external game APIs.

Never substitutes stubs for game assemblies. Tests use the repository's simulated
game implementation and are reported separately from server/game validation.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess

p = argparse.ArgumentParser()
p.add_argument('--mono-root', type=Path, required=True)
p.add_argument('--framework', type=Path, required=True)
p.add_argument('--host-mif', type=Path, required=True)
p.add_argument('--managed', type=Path, required=True)
p.add_argument('--output', type=Path, default=Path('artifacts/qa'))
p.add_argument('--state-dotnet', type=Path, help='Optional simulated-test runtime for Unity APIs using netstandard 2.1; does not change production references.')
args = p.parse_args()
repo = Path(__file__).resolve().parents[1]
out = args.output.resolve()
out.mkdir(parents=True, exist_ok=True)
mono_root = args.mono_root.resolve()
mono = str(mono_root / 'usr/bin/mono-sgen')
compiler = str(mono_root / 'usr/lib/mono/4.5/mcs.exe')
env = dict(os.environ, MONO_PATH=str(mono_root / 'usr/lib/mono/4.5'),
           LD_LIBRARY_PATH=str(mono_root / 'usr/lib'), MONO_CFG_DIR=str(mono_root / 'etc'))
refs = [args.framework.resolve()/n for n in ('mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll')]
host_refs = refs + [args.host_mif.resolve()]
state_refs = refs + [args.managed.resolve()/n for n in ('Mif.dll','ModApi.dll','UnityEngine.CoreModule.dll')]
facade = args.framework.resolve()/'Facades/netstandard.dll'
state_refs.append(facade)
for f in host_refs+state_refs:
    if not f.is_file():
        raise SystemExit('Missing real reference: '+str(f))
shared = repo/'src/Shared/Localization.cs'
host = [repo/'src/QuantumHangarQc'/n for n in ('QuantumHangarQc.cs','LiveState.cs','Markers.cs','HangarUi.cs')]+[shared]
state = [repo/'src/QuantumHangarQcState'/n for n in ('LiveState.cs','StateMod.cs','Markers.cs','GpsBridge.cs','HangarUi.cs','UiBridge.cs')]+[shared]
records = []
def run(name, cmd):
    r = subprocess.run(cmd, env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (out/(name+'.log')).write_text(r.stdout)
    print(name, 'PASS' if r.returncode == 0 else 'FAIL', r.stdout.strip()[-1500:])
    records.append(dict(test=name, status='PASS' if r.returncode == 0 else 'FAIL', output=r.stdout, method='compilation' if name.startswith('compile') else 'simulated API / local filesystem'))
    if r.returncode:
        (out/'results.json').write_text(json.dumps(records, indent=2))
        raise SystemExit(r.returncode)
def compile(name, directory, filename, sources, references, executable=False, main=None):
    directory.mkdir(parents=True, exist_ok=True)
    cmd = [mono,compiler,'-nologo','-noconfig','-nostdlib','-target:'+('exe' if executable else 'library'),'-out:'+str(directory/filename),'-resource:'+str(repo/'localization/en.xml')+',QH.en.xml']
    cmd += ['-r:'+str(f) for f in references] + ([ '-main:'+main ] if main else []) + [str(f) for f in sources]
    run('compile_'+name,cmd)
    for f in references[len(refs):]:
        if f.name!='netstandard.dll':shutil.copyfile(f,directory/f.name)
    shutil.copytree(repo/'localization',directory/'Languages',dirs_exist_ok=True)
    return directory/filename
def state_run(exe, world):
    if args.state_dotnet:
        exe.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
        return [str(args.state_dotnet.resolve()),str(exe),str(world)]
    return [mono,str(exe),str(world)]
host_dir=out/'host'; state_dir=out/'state'
compile('host',host_dir,'QuantumHangarQc.dll',host,host_refs)
compile('state',state_dir,'QuantumHangarQcState.dll',state,state_refs)
protobuf=args.host_mif.resolve().parent/'protobuf-net.dll'
if not protobuf.is_file():
    raise SystemExit('Missing test runtime dependency: '+str(protobuf))
shutil.copyfile(protobuf,host_dir/protobuf.name)
shutil.copyfile(protobuf,state_dir/protobuf.name)
host_tests=compile('host_tests',host_dir,'HostTests.exe',host+[repo/'src/QuantumHangarQc/Tests.cs'],host_refs,True,'Tests')
run('host_regression',[mono,str(host_tests),str(out/'host_worlds')])
state_tests=compile('state_tests',state_dir,'StateTests.exe',state+[repo/'src/QuantumHangarQcState/Tests/Stubs.cs',repo/'src/QuantumHangarQcState/Tests/Tests.cs'],state_refs,True,'QhStateTests')
run('state_regression',state_run(state_tests,out/'state_worlds'))
ui_tests=compile('ui_tests',state_dir,'UiTests.exe',state+[repo/'src/QuantumHangarQcState/Tests/Stubs.cs',repo/'src/QuantumHangarQcState/Tests/UiTests.cs'],state_refs,True,'UiTests')
run('ui_regression',state_run(ui_tests,out/'ui_worlds'))
locale_tests=compile('locale_tests',host_dir,'LocaleTests.exe',host+[repo/'tests/LocaleTests.cs',repo/'src/QuantumHangarQc/Tests.cs'],host_refs,True,'LocaleTests')
run('localization',[mono,str(locale_tests),str(repo/'localization'),str(out/'locale_worlds')])
fallback_tests=compile('fallback_tests',host_dir,'FallbackTests.exe',[shared,repo/'src/QuantumHangarQc/LiveState.cs',repo/'tests/FallbackTests.cs'],refs,True,'FallbackTests')
run('fallback_cache',[mono,str(fallback_tests),str(repo/'localization'),str(out/'fallback_worlds')])
compile('native_chat_probe',state_dir,'NativeChatProbe.dll',[repo/'experiments/NativeChatProbe.cs'],state_refs)
ref_evidence=[dict(name=f.name,sha256=hashlib.sha256(f.read_bytes()).hexdigest()) for f in dict.fromkeys(host_refs+state_refs)]
(out/'results.json').write_text(json.dumps(dict(version='0.2.6',tests=records,references=ref_evidence,game_tests='NOT_RUN',windows_startup='NOT_RUN',simulated_state_runtime='net8.0' if args.state_dotnet else 'Mono'),indent=2)+'\n')
print('All local checks passed. Windows/game checks NOT_RUN.')
