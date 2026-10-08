#!/usr/bin/env python3
"""Package only QH update files; verify compiled hashes before distributing."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import zipfile

p=argparse.ArgumentParser()
p.add_argument('--binaries',type=Path,required=True,help='verify.py output directory')
p.add_argument('--output',type=Path,required=True)
p.add_argument('--commit',required=True)
a=p.parse_args()
if not re.fullmatch('[0-9a-f]{40}',a.commit):raise SystemExit('Exact commit SHA required')
repo=Path(__file__).resolve().parents[1]
audit=json.loads((repo/'docs/qa/BINARY_AUDIT_0.2.6.json').read_text())
files={}
for kind,folder,name,target in [('host','host','QuantumHangarQc.dll','ModLoader/MODs/QuantumHangarQc'),('state','state','QuantumHangarQcState.dll','QuantumHangarQcState')]:
    binary=a.binaries/folder/name
    if hashlib.sha256(binary.read_bytes()).hexdigest()!=audit[kind]['sha256']:raise SystemExit('Binary differs from tested audit: '+name)
    files[target+'/'+name]=binary.read_bytes()
    for catalog in sorted((repo/'localization').glob('*.xml')):files[target+'/Languages/'+catalog.name]=catalog.read_bytes()
files['QuantumHangarQcState/QuantumHangarQcState_Info.yaml']=(repo/'config/QuantumHangarQcState_Info.yaml').read_bytes()
prefix='ModLoader/MODs/QuantumHangarQc/'
files[prefix+'TEST_VERSION.json']=json.dumps({'version':'0.2.6','branch':'validate','commit':a.commit,'base_commit':'623c0bc2156cd957ee514193c71d248a9fb936e8','binary_audit':audit,'windows_startup':'NOT_RUN','real_game':'NOT_RUN','automatic_language':'BLOCKED'},indent=2).encode()+b'\n'
files[prefix+'UPDATE_README_FR_EN.txt']=(repo/'docs/INSTALLATION_FR_EN.md').read_bytes()
files[prefix+'VALIDATION_0.2.6.md']=(repo/'docs/VALIDATION.md').read_bytes()
files[prefix+'SHA256SUMS.txt']=''.join(hashlib.sha256(data).hexdigest()+'  '+path+'\n' for path,data in sorted(files.items())).encode()
a.output.parent.mkdir(parents=True,exist_ok=True)
with zipfile.ZipFile(a.output,'w',zipfile.ZIP_DEFLATED) as z:
    for name,data in sorted(files.items()):z.writestr(name,data)
with zipfile.ZipFile(a.output) as z:
    assert z.testzip() is None
    assert {n.split('/')[0] for n in z.namelist()}=={'ModLoader','QuantumHangarQcState'}
    assert len([n for n in z.namelist() if n.endswith('.dll')])==2
    assert len([n for n in z.namelist() if '/Languages/' in n])==16
    assert not any(Path(n).name in ('Configuration.xml','DllNames.txt','Mif.dll','ModApi.dll') for n in z.namelist())
print(json.dumps({'file':str(a.output.resolve()),'sha256':hashlib.sha256(a.output.read_bytes()).hexdigest(),'files':len(files),'commit':a.commit,'inspection':'PASS'}))
