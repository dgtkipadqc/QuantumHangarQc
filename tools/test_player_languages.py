#!/usr/bin/env python3
"""Real C# compilation + simulated game API/local-file tests; no live server access."""
import argparse, hashlib, json, os, shutil, subprocess
from pathlib import Path
p = argparse.ArgumentParser()
p.add_argument('--mono-root', type=Path, required=True)
p.add_argument('--framework', type=Path, required=True)
p.add_argument('--host-deps', type=Path, required=True)
p.add_argument('--output', type=Path, default=Path('artifacts/player-languages'))
a = p.parse_args()
repo = Path(__file__).resolve().parents[1]
out = a.output.resolve(); out.mkdir(parents=True, exist_ok=False)
mono_root = a.mono_root.resolve()
mono = str(mono_root/'usr/bin/mono-sgen')
env = dict(os.environ, MONO_PATH=str(mono_root/'usr/lib/mono/4.5'),
           MONO_CFG_DIR=str(mono_root/'etc'), LD_LIBRARY_PATH=str(mono_root/'usr/lib'))
refs = [a.framework.resolve()/n for n in ('mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll')]
refs += [a.host_deps.resolve()/'Mif.dll', a.host_deps.resolve()/'ModExtenderCommunication.dll']
for f in refs:
    if not f.is_file(): raise SystemExit('Missing real reference: '+str(f))
for name in ('Mif.dll','ModExtenderCommunication.dll','protobuf-net.dll'):
    shutil.copyfile(a.host_deps/name, out/name)
shutil.copytree(repo/'localization', out/'Languages')
shutil.copyfile(repo/'config/Configuration.example.xml',out/'Configuration.xml')
sources = sorted(f for f in (repo/'src/QuantumHangarQc').glob('*.cs') if f.name != 'Tests.cs')
sources += [repo/'src/Shared/Localization.cs']
base = [mono,str(mono_root/'usr/lib/mono/4.5/mcs.exe'),'-noconfig','-nostdlib+']
base += ['-r:'+str(f) for f in refs]+['-resource:'+str(repo/'localization/en.xml')+',QH.en.xml']
records = []
evidence = {'method':'Real C# compilation; simulated game API, real local files',
            'live_game':'NON TESTÉ','windows_startup':'NON TESTÉ',
            'source_sha256':{str(f.relative_to(repo)):hashlib.sha256(f.read_bytes()).hexdigest() for f in sources},
            'reference_sha256':{str(f):hashlib.sha256(f.read_bytes()).hexdigest() for f in refs},'tests':records}
def run(name, cmd):
    r = subprocess.run(cmd,env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    (out/(name+'.log')).write_text(r.stdout)
    records.append({'name':name,'status':'PASS' if r.returncode==0 else 'FAIL','command':cmd,'output':r.stdout})
    (out/'results.json').write_text(json.dumps(evidence,ensure_ascii=False,indent=2)+'\n')
    print(name,records[-1]['status'],r.stdout.strip()[-1200:],flush=True)
    if r.returncode: raise SystemExit(r.returncode)
run('compile_host',base+['-target:library','-out:'+str(out/'QuantumHangarQc.dll')]+[str(f) for f in sources])
for name, extra, args in (
    ('Tests',[repo/'src/QuantumHangarQc/Tests.cs'],[out/'host-worlds']),
    ('LocaleTests',[repo/'src/QuantumHangarQc/Tests.cs',repo/'tests/LocaleTests.cs'],[repo/'localization',out/'locale-worlds']),
    ('ConfigTests',[repo/'tests/ConfigTests.cs'],[out/'config-worlds'])):
    exe=out/(name+'.exe')
    run('compile_'+name,base+['-main:'+name,'-out:'+str(exe)]+[str(f) for f in sources+extra])
    run(name,[mono,str(exe)]+[str(x) for x in args])
