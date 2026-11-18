"""Reproducible original Skyline courses and a seamless 32-bar stereo soundtrack."""
from pathlib import Path
import json, math, shutil, wave, struct, zlib
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Assets/_BallQuest'

def vec(x, y, z): return dict(x=round(x, 4), y=round(y, 4), z=round(z, 4))

def course(number, title, offset, mirror):
    base = 28 + offset
    rise=[4,5,7,4,9,6][number-1]
    first_drop=[7,8,10,8,12,11][number-1]
    last_drop=[9,11,13,12,16,15][number-1]
    high=base+rise;low=high-first_drop;bottom=base-last_drop
    tracks, obstacles, pickups = [], [], []
    def road(a, b, width, rails=True, style=0):
        a, b = np.array(a, dtype=float), np.array(b, dtype=float)
        d = b-a
        yaw = math.atan2(d[0], d[2]); pitch = -math.atan2(d[1], math.hypot(d[0], d[2]))
        normal = np.array([math.sin(yaw)*math.sin(pitch), math.cos(pitch), math.cos(yaw)*math.sin(pitch)])
        center = (a+b)/2-normal*.4
        tracks.append(dict(position=vec(*center), size=vec(width,.8,np.linalg.norm(d)+.12),
                           rotation=vec(math.degrees(pitch),math.degrees(yaw),0), style=style,rails=rails))
        for t in np.arange(.14,.96,.2):
            p=a+d*t+np.array([0,.85,0]); pickups.append(dict(kind=0,position=vec(*p)))
    x=[3,4.8,5.8,6.2,4,7][number-1]*mirror
    road((0,base,0),(0,base,24),8)
    road((0,base,24),(x,high,46),6.5,True,1)
    road((x,high,46),(x,high,67),6,True)
    road((x,high,67),(x,high,78),5 if number<4 else 3.8,False)
    # Deliberate free falls. Long, wide catch platforms accommodate the turbo trajectory.
    road((x,low,76),(x,low,112),10,True)
    road((x,low,112),(-x,low,134),6 if number<4 else 4.8,True,2)
    road((-x,low,134),(-x,base,154),5.6,True,1)
    road((-x,base,154),(-x,base,176),5 if number<3 else 3.8,False)
    road((-x,bottom,174),(-x,bottom,212),10,True)
    road((-x,bottom,212),(0,bottom,234),6,True,2)
    road((0,bottom,234),(0,bottom,253),10,True)
    def obstacle(kind,x,y,z,size,axis=(1,0,0),amplitude=2,speed=1,phase=0):
        obstacles.append(dict(kind=kind,position=vec(x,y,z),size=vec(*size),axis=vec(*axis),amplitude=amplitude,speed=speed,phase=phase))
    obstacle(6,x,high+.18,56,(5.3,.32,4),axis=(0,0,1))
    obstacle(6,-x,base+.18,162,(3.5,.32,4),axis=(0,0,1))
    obstacle(1,0,low+1,123,(1.4,2,1),amplitude=2.2,speed=.85+number*.08)
    obstacle(0,-x*.5,bottom+1,224,(4.5,.65,.7),speed=.8+number*.08)
    if number>1: obstacle(1,x,high+.85,64,(1.1,1.7,1),amplitude=1.8,speed=1,phase=number)
    if number>2: obstacle(1,-x,base+.9,157,(1,1.8,1),amplitude=1.2,speed=.7,phase=number*.6)
    pickups.extend([dict(kind=1,position=vec(x,low+1.1,103)),dict(kind=2,position=vec(-x,bottom+1,203))])
    return dict(id=number,title=title,hint='Swipe up to roll. Lift to coast. Swipe sideways to steer; down to slow.',world=0,
        spawn=vec(0,base+.65,3),finish=vec(0,bottom+.7,247),
        checkpoints=[vec(x,high+.65,69),vec(x,low+.65,108),vec(-x,base+.65,155),vec(-x,bottom+.65,207)],
        tracks=tracks,obstacles=obstacles,pickups=pickups,parSeconds=65+number*3,killHeight=bottom-10,allowJump=False)

levels=[course(i+1,name,i*2,1 if i%2==0 else -1) for i,name in enumerate([
    'Skyline Drift','Apricot Rush','Cloud Nine','Violet Velocity','Sunset Freefall','Above It All'])]
(ASSETS/'Content/SkylineCampaign.json').write_text(json.dumps(dict(levels=levels),indent=2))

# A composed instrumental, not a short repeating alert: 32 bars / 68.57 seconds at 112 BPM.
rate=32000; beat=60/112; length=beat*128; n=round(rate*length)
mix=np.zeros((n,2),np.float64); rng=np.random.default_rng(3417)
def note(midi): return 440*2**((midi-69)/12)
def add(sound,start,pan=0,gain=1):
    start=round(start*rate)%n; sound=np.asarray(sound)*gain
    stereo=np.column_stack((sound*math.sqrt((1-pan)/2),sound*math.sqrt((1+pan)/2)))
    first=min(len(stereo),n-start);mix[start:start+first]+=stereo[:first]
    if first<len(stereo): mix[:len(stereo)-first]+=stereo[first:]
def tone(midi,duration,kind):
    t=np.arange(round(rate*duration))/rate; f=note(midi)
    if kind=='pad':
        env=np.minimum(t/.32,1)*np.minimum((duration-t)/.65,1)
        return (np.sin(2*np.pi*f*t)+.28*np.sin(2*np.pi*f*2.002*t)+.14*np.sin(2*np.pi*f*.998*t))*env*.045
    if kind=='bass':
        return (np.sin(2*np.pi*f*t)+.18*np.sin(2*np.pi*f*2*t))*np.minimum(t/.012,1)*np.minimum((duration-t)/.07,1)*.17
    env=(1-np.exp(-t*200))*np.exp(-t*5.5)
    return (np.sin(2*np.pi*f*t)+.27*np.sin(2*np.pi*f*3*t)*np.exp(-t*9)+.12*np.sin(2*np.pi*f*4*t))*env*.17
chords=[[57,60,64,67],[53,57,60,64],[48,52,55,59],[55,59,62,65]]
lead=[76,0,79,81,0,79,76,72,74,0,77,79,0,76,74,71]
for bar in range(32):
    start=bar*4*beat; chord=chords[(bar//2)%4]
    for j,midi in enumerate(chord): add(tone(midi,beat*4.4,'pad'),start,pan=(j-1.5)/2,gain=.85)
    for b in range(4):
        root=chord[0]-24
        add(tone(root if b!=3 else root+12,beat*.68,'bass'),start+b*beat)
        t=np.arange(round(rate*.3))/rate
        kick=np.sin(2*np.pi*(48*t+62*.026*(1-np.exp(-t/.026))))*np.exp(-t*16)*.55
        if bar>=4: add(kick,start+b*beat)
        for off in (0,.5):
            t=np.arange(round(rate*.09))/rate; noise=rng.normal(0,1,len(t)); high=np.diff(noise,prepend=0)
            if bar>=2: add(high*np.exp(-t*75)*.035,start+(b+off)*beat,pan=.25 if off else -.2)
        if b%2==1 and bar>=4:
            t=np.arange(round(rate*.16))/rate
            clap=rng.normal(0,1,len(t))*np.exp(-t*27)*(.7+.3*np.cos(2*np.pi*42*t))*.075
            add(clap,start+b*beat,pan=-.12)
    for step in range(8):
        midi=chord[[0,2,1,3,2,1,3,2][step]]+12
        add(tone(midi,beat*.9,'bell'),start+step*.5*beat,pan=.42*math.sin(step*1.5),gain=.46 if bar<8 else .7)
    if bar>=8:
        for step in range(4):
            midi=lead[(bar%4)*4+step]
            if midi: add(tone(midi,beat*1.5,'bell'),start+step*beat,pan=-.1,gain=.8)
dry=mix.copy()
for delay,gain in [(beat*.75,.18),(beat*1.5,.09),(.107,.06),(.193,.04)]:
    mix+=np.roll(dry,round(delay*rate),axis=0)[:,::-1]*gain
mix=np.tanh(mix*1.35); mix*=.88/max(.88,np.abs(mix).max())
folder=ASSETS/'Audio/Music';folder.mkdir(parents=True,exist_ok=True)
with wave.open(str(folder/'Skyline Reverie.wav'),'wb') as f:
    f.setnchannels(2);f.setsampwidth(2);f.setframerate(rate);f.writeframes((mix*32767).astype('<i2').tobytes())
fontfolder=ASSETS/'Art/Fonts';fontfolder.mkdir(parents=True,exist_ok=True)
source=Path('C:/Users/soura/Desktop/Game/node_modules/@expo-google-fonts/fredoka')
shutil.copyfile(source/'600SemiBold/Fredoka_600SemiBold.ttf',fontfolder/'Fredoka-SemiBold.ttf')
shutil.copyfile(source/'LICENSE_FONT',fontfolder/'Fredoka-LICENSE.txt')

# Original launcher mark, constructed from the same ball/skyway geometry as the native UI.
size=512; yy,xx=np.mgrid[0:size,0:size];u=xx/size;v=yy/size
pixels=np.zeros((size,size,3),float)
pixels[:]=np.array([.34,.25,.49])*(1-v[...,None]*.25)+np.array([.11,.07,.13])*u[...,None]
surface=(yy>398-xx*.31)&(yy<487-xx*.31)
pixels[surface]=[.98,.89,.76]
fascia=(yy>=487-xx*.31);pixels[fascia]=[.64,.48,.76]
shadow=((xx-264)/151)**2+((yy-397)/34)**2
pixels*=1-np.exp(-shadow*2)[...,None]*.26
x=(xx-257)/143;y=-(yy-241)/143;r=x*x+y*y;inside=r<1
z=np.sqrt(np.maximum(0,1-r));light=np.maximum(0,-x*.43+y*.55+z*.72)
base=np.zeros_like(pixels);base[:]=[.91,.13,.29]
band=np.abs(y+x*.30)<.12;base[band]=[1,.97,.89]
ball=base*(.35+.65*light[...,None])+np.maximum(0,-x*.35+y*.45+z*.85)[...,None]**46*.5
pixels[inside]=ball[inside]
pixels=(np.clip(pixels,0,1)*255).astype(np.uint8)
def chunk(kind,data):return struct.pack('>I',len(data))+kind+data+struct.pack('>I',zlib.crc32(kind+data)&0xffffffff)
png=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',size,size,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(b''.join(b'\0'+row.tobytes() for row in pixels),9))+chunk(b'IEND',b'')
(ASSETS/'Art/Textures/Skyline Icon.png').write_bytes(png)
print(f'Created {len(levels)} elevated courses; {length:.2f}s original stereo soundtrack; licensed Fredoka font.')
