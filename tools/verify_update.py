import os,subprocess,shutil,json,hashlib,uuid,argparse
from pathlib import Path
parser=argparse.ArgumentParser(description="QH net472 build and focused simulated regression tests; no real-game certification.")
parser.add_argument('--deps',type=Path,required=True,help='Directory containing mono/, net472/, loader/ModLoader/Host/ and native/ real game assemblies')
parser.add_argument('--output',type=Path,required=True)
args=parser.parse_args();repo=Path(__file__).resolve().parents[1];deps=args.deps.resolve();out=args.output.resolve();out.mkdir(parents=True,exist_ok=True)
mono=deps/'mono/usr/bin/mono-sgen';compiler=deps/'mono/usr/lib/mono/4.5/mcs.exe'
env=dict(os.environ,MONO_PATH=str(deps/'mono/usr/lib/mono/4.5'),MONO_CFG_DIR=str(deps/'mono/etc'))
framework=deps/'net472/build/.NETFramework/v4.7.2'
refs=[framework/n for n in ['mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll']]
hostref=deps/'loader/ModLoader/Host/Mif.dll'
shared=repo/'src/Shared/Localization.cs'
host=sorted(p for p in (repo/'src/QuantumHangarQc').glob('*.cs') if p.name!='Tests.cs')+[shared]
state=sorted((repo/'src/QuantumHangarQcState').glob('*.cs'))+[shared]
records=[]
def run(name,cmd):
 r=subprocess.run(list(map(str,cmd)),env=env,capture_output=True,text=True);s=r.stdout+r.stderr;(out/(name+'.log')).write_text(s);print(name,r.returncode,s[-2500:],flush=True)
 records.append(dict(name=name,passed=r.returncode==0,output=s))
 (out/'results.json').write_text(json.dumps(records,indent=2))
 if r.returncode:raise SystemExit(r.returncode)
def compile(name,sources,extra=(),main=None):
 d=out/name;d.mkdir(exist_ok=True);exe=d/(name+('.exe' if main else '.dll'))
 run('compile_'+name,[mono,compiler,'-nologo','-noconfig','-nostdlib+','-target:'+('exe' if main else 'library'),'-out:'+str(exe),'-resource:'+str(repo/'localization/en.xml')+',QH.en.xml']+(['-main:'+main] if main else [])+['-r:'+str(x) for x in refs+list(extra)]+sources)
 if name=='ConfigTests':shutil.copyfile(repo/'config/Configuration.example.xml',d/'Configuration.xml')
 for f in extra:shutil.copyfile(f,d/f.name)
 shutil.copyfile(deps/'loader/ModLoader/Host/protobuf-net.dll',d/'protobuf-net.dll')
 shutil.copytree(repo/'localization',d/'Languages',dirs_exist_ok=True)
 return exe
compile('QuantumHangarQc',host,[hostref])
compile('QuantumHangarQcState',state,[deps/'native/Mif.dll',deps/'native/ModApi.dll'])
tests=repo/'src/QuantumHangarQc/Tests.cs'
def test(name,sources,main,args,extra=(hostref,)):
 exe=compile(name,sources,extra,main);world=out/(name+'_world_'+uuid.uuid4().hex);run(name,[mono,exe]+args+[world])
test('HostTests',host+[tests],'Tests',[])
test('LocaleTests',host+[tests,repo/'tests/LocaleTests.cs'],'LocaleTests',[repo/'localization'])
ui=host+[tests,repo/'src/QuantumHangarQcState/UiBridge.cs',repo/'tests/LanguageMenuTests.cs',repo/'tests/fixtures/LegacyHangarUi.cs',repo/'tests/fixtures/LegacyUiBridge.cs']
test('LanguageMenuTests',ui,'LanguageMenuTests',[])
if (repo/'tests/ConfirmationTests.cs').exists():test('ConfirmationTests',ui+[repo/'tests/ConfirmationTests.cs'],'ConfirmationTests',[])

test("TerrainTests",[repo/"src/QuantumHangarQc/LiveState.cs",repo/"src/QuantumHangarQcState/TerrainBridge.cs",repo/"tests/TerrainTests.cs"],"TerrainTests",[],extra=())

test("ConfigTests",host+[tests,repo/"tests/ConfigTests.cs"],"ConfigTests",[],extra=(hostref,deps/"loader/ModLoader/Host/ModExtenderCommunication.dll"))

test("MarkerTests",host+[tests,repo/"tests/MarkerTests.cs"],"MarkerTests",[])

test("RestoreTests",host+[tests,repo/"tests/RestoreTests.cs"],"RestoreTests",[])
