"""Convert licensed FlightGear AC3D meshes to Unity-compatible OBJ/MTL.
Preserves named parts, hierarchical transforms, material slots and corner UVs.
"""
from pathlib import Path
import json, shlex, shutil, zipfile
import numpy as np

PROJECT = Path(__file__).resolve().parents[1]
cache = PROJECT / 'Temp/F5Source'
SOURCE = next(cache.glob('F-5E-*'), None)
if SOURCE is None:
    cache.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(Path(__file__).parent / 'F5E-original-source.zip') as archive:
        for entry in archive.infolist():
            target = (cache / entry.filename).resolve()
            if not target.is_relative_to(cache.resolve()):
                raise ValueError('Invalid source archive path')
        archive.extractall(cache)
    SOURCE = next(cache.glob('F-5E-*'))
DEST = PROJECT / 'Assets/Blueprinter/Mods/F5/Models'
DEST.mkdir(parents=True, exist_ok=True)

def parse_ac(path):
    lines = iter(path.read_text(encoding='utf-8', errors='replace').splitlines())
    assert next(lines).startswith('AC3D'), path
    materials=[]
    def node(first):
        obj={'type':first.split()[1], 'name':'part', 'loc':[0,0,0], 'rot':np.eye(3), 'vertices':[], 'surfaces':[], 'children':[], 'texture':None}
        while True:
            line=next(lines); parts=shlex.split(line)
            if not parts: continue
            key=parts[0]
            if key=='name': obj['name']=parts[1]
            elif key=='loc': obj['loc']=list(map(float,parts[1:]))
            elif key=='rot': obj['rot']=np.array(list(map(float,parts[1:]))).reshape(3,3)
            elif key=='texture': obj['texture']=parts[1]
            elif key=='numvert': obj['vertices']=[list(map(float,next(lines).split())) for _ in range(int(parts[1]))]
            elif key=='numsurf':
                for _ in range(int(parts[1])):
                    flags=0; mat=0
                    while True:
                        p=next(lines).split()
                        if p[0]=='SURF': flags=int(p[1],0)
                        elif p[0]=='mat': mat=int(p[1])
                        elif p[0]=='refs':
                            refs=[list(map(float,next(lines).split())) for _ in range(int(p[1]))]
                            obj['surfaces'].append((flags,mat,refs)); break
            elif key=='data':
                if int(parts[1])>0: next(lines) # Blender stores a single data line
            elif key=='kids':
                obj['children']=[]
                for _ in range(int(parts[1])):
                    first=next(lines, None)
                    if first is None:
                        if obj['type']!='world': raise ValueError('Truncated AC3D object '+obj['name'])
                        print('Source world child-count exceeds actual objects; all available objects retained')
                        break
                    while not first.startswith('OBJECT '): first=next(lines)
                    obj['children'].append(node(first))
                return obj
    for line in lines:
        if line.startswith('MATERIAL '):
            p=shlex.split(line)
            materials.append({'rgb':list(map(float,p[p.index('rgb')+1:p.index('rgb')+4])), 'alpha':1-float(p[p.index('trans')+1])})
        elif line.startswith('OBJECT '): return materials,node(line)
    raise ValueError(f'No object in {path}')

stats=[]
for path in [SOURCE/'Models/f5emin.ac']:
    if any(p in path.parts for p in ('Effects','Lights','Payload','Pilot')): continue
    materials,root=parse_ac(path)
    rel=path.relative_to(SOURCE/'Models')
    target=DEST/rel.with_suffix('.obj'); target.parent.mkdir(parents=True,exist_ok=True)
    output=[f'# Adapted from FGMEMBERS-NONGPL/F-5E, CC-BY-NC-SA-3.0; see CREDITS.md',f'mtllib {target.with_suffix(".mtl").name}']
    material_defs={}; vi=1; ti=1; part_names=[]; triangle_count=0; points=[]
    def write(obj,parent=np.eye(4)):
        global vi,ti,triangle_count
        transform=np.eye(4);transform[:3,:3]=obj['rot'];transform[:3,3]=obj['loc'];transform=parent@transform
        name=obj['name'].replace(' ','_');part_names.append(name);output.append('g '+name)
        for v in obj['vertices']:
            p=transform@np.array([*v,1]);u=(p[2],p[1],-p[0]);points.append(u);output.append('v '+' '.join(f'{x:.7g}' for x in u))
        for flags,mat,refs in obj['surfaces']:
            if flags & 0xF or len(refs)<3: continue
            key=(mat,obj['texture']);mname=f'{target.stem}_material_{len(material_defs)}'
            if key not in material_defs: material_defs[key]=mname
            output.append('usemtl '+material_defs[key]);output.append('s 1' if flags&0x10 else 's off')
            corner=[]
            for idx,u,v in refs:
                assert 0<=int(idx)<len(obj['vertices'])
                output.append(f'vt {u:.7g} {v:.7g}');corner.append(f'{vi+int(idx)}/{ti}');ti+=1
            for j in range(1,len(corner)-1):
                output.append('f '+' '.join([corner[0],corner[j],corner[j+1]]));triangle_count+=1
        vi+=len(obj['vertices'])
        for child in obj['children']: write(child,transform)
    write(root)
    target.write_text('\n'.join(output)+'\n',encoding='utf-8')
    mtl=[]
    for (idx,tex),name in material_defs.items():
        m=materials[idx];mtl.extend([f'newmtl {name}','Kd '+' '.join(map(str,m['rgb'])),f'd {m["alpha"]}','illum 2'])
        if tex:
            source_tex=path.parent/tex
            if not source_tex.is_file(): raise FileNotFoundError(source_tex)
            tex_dest=target.parent/source_tex.name
            shutil.copy2(source_tex,tex_dest);mtl.append('map_Kd '+tex_dest.name)
    target.with_suffix('.mtl').write_text('\n'.join(mtl)+'\n',encoding='utf-8')
    bounds=[np.min(points,axis=0).tolist(),np.max(points,axis=0).tolist()] if points else None
    stats.append({'mesh':str(rel),'vertices':vi-1,'triangles':triangle_count,'parts':part_names,'bounds':bounds})
(DEST/'mesh-report.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
print(json.dumps({'models':len(stats),'triangles':sum(s['triangles'] for s in stats),'destination':str(DEST)},indent=2))


