"""Bake contact clips from CC0 real-world recordings. No oscillators/noise generators.
Requires workspace-local soundfile/scipy; exact inputs and processing retained in JSON.
"""
from pathlib import Path
import sys, json, hashlib
ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'.tooling/audio-python'))
import numpy as np
import soundfile as sf
from scipy import signal
RATE=22050
SOURCE=ROOT/'Documentation/References/V10/AudioSources'
DEST=ROOT/'Assets/_BallQuest/Audio/SFX/NaturalContactV10'
DEST.mkdir(parents=True,exist_ok=True)
SOURCES={
 'MetalRoll':('slamsal','697979','Metal ball rolling on wood'),
 'RubberRoll':('89o_dev','869171','Bouncy rubber ball rolling on floor'),
 'RubberBounce':('SomeoneCool15','423778','Large rubber ball bouncing'),
 'MetalHit':('RyanKingArt','620077','Metal impact'),
 'WoodHit':('Ian_G','547414','Wood impact'),
}
def read(name):
    data,rate=sf.read(SOURCE/(name+'.mp3'),always_2d=True)
    data=data.mean(axis=1); divisor=np.gcd(RATE,rate)
    return signal.resample_poly(data,RATE//divisor,rate//divisor)
data={name:read(name) for name in SOURCES}
def band(x,lo,hi):
    return signal.sosfiltfilt(signal.butter(3,[lo,hi],btype='bandpass',fs=RATE,output='sos'),x)
def normalize(x,rms,peak=.7):
    x=x-np.mean(x);x=x*(rms/max(1e-8,np.sqrt(np.mean(x*x))))
    x*=min(1,peak/max(1e-8,np.max(np.abs(x))))
    return x
def loop(x,seconds=3.2):
    # Equal-power overlapped head/tail, preserving the recorded irregular contact.
    n=int(.12*RATE);theta=np.linspace(0,np.pi/2,n)
    core=np.r_[x[n:-n],x[-n:]*np.cos(theta)+x[:n]*np.sin(theta)]
    # Keep complete periods: trimming a repeated recording at an arbitrary
    # duration would put unrelated waveform samples across the playback seam.
    return np.tile(core,max(2,round(seconds*RATE/len(core))))
def impact(name,lo,hi,pitch=1):
    x=data[name];energy=np.convolve(x*x,np.ones(220)/220,mode='same')
    peak=int(np.argmax(energy));start=max(0,peak-int(.006*RATE))
    x=x[start:start+int(.32*RATE)]
    if pitch!=1:x=signal.resample(x,int(len(x)/pitch))
    x=np.pad(x,(0,max(0,int(.34*RATE)-len(x))))[:int(.34*RATE)]
    x=band(x,lo,hi)*np.exp(-np.arange(len(x))/RATE*8)
    x[:40]*=np.linspace(0,1,40);x[-220:]*=np.linspace(1,0,220)
    return normalize(x,.095,.7)
metal=data['MetalRoll'][int(.85*RATE):int(2.08*RATE)]
rubber=data['RubberRoll'][int(5.3*RATE):int(7.25*RATE)]
configs={
 'Metal':(metal,80,1500,'MetalHit',100,6500,1.0),
 'Wood':(metal,70,700,'WoodHit',70,3000,1.0),
 'Glass':(metal,190,2700,'MetalHit',550,8500,1.55),
 'Rubber':(rubber,60,520,'RubberBounce',45,1800,1.0),
 'Stone':(metal,55,900,'WoodHit',60,3800,.78),
 'Energy':(rubber,100,1100,'MetalHit',200,4800,1.2),
}
surfaces={'Wood':(80,4200,.20),'Metal':(140,7000,.24),'Stone':(70,3400,.16),'Ice':(180,6400,.12),'Sand':(65,1000,.10),'Glass':(250,7500,.20),'Grass':(65,900,.08),'Boost':(100,3200,.12)}
metrics=[];audition=[]
def write(name,x):
    path=DEST/(name+'.wav');sf.write(path,x,RATE,subtype='PCM_16')
    metrics.append({'name':name,'rms':float(np.sqrt(np.mean(x*x))),'peak':float(np.max(np.abs(x))),'seconds':len(x)/RATE,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
for material,(source,lo,hi,hit,ilo,ihi,pitch) in configs.items():
    rolling=loop(normalize(band(source,lo,hi),.075))
    sliding=loop(normalize(band(source,max(90,lo),min(3500,hi*1.7)),.055))
    body=impact(hit,ilo,ihi,pitch)
    write('NaturalV10_'+material+'_Rolling',rolling)
    write('NaturalV10_'+material+'_Sliding',sliding)
    write('NaturalV10_'+material+'_Impact',body)
    audition.extend([rolling[:2*RATE]*.7,np.zeros(RATE//4),body,np.zeros(RATE//2)])
    for surface,(low,high,mix) in surfaces.items():
        floor=impact('MetalHit' if surface in ('Metal','Glass','Ice') else 'WoodHit',low,high)
        # Preserve the ball's timbre on every floor instead of replacing it.
        pair=normalize(body*(1-mix)+floor*mix,.095)
        write('NaturalV10_'+material+'_On_'+surface,pair)
out=ROOT/'TestResults/Audio';out.mkdir(parents=True,exist_ok=True)
sf.write(out/'NaturalContactV10-Audition.wav',np.concatenate(audition),RATE,subtype='PCM_16')
report={'license':'CC0-1.0','method':'Mono resampling, band limiting, selected contact segments, normalized dynamics and overlapped loop seams; impacts combine ball and struck surface. Glass/stone/energy are processed interpretations, not recordings of those exact balls.','sources':[{'file':n+'.mp3','author':a,'url':f'https://freesound.org/people/{a}/sounds/{i}/','description':d,'sha256':hashlib.sha256((SOURCE/(n+'.mp3')).read_bytes()).hexdigest()} for n,(a,i,d) in SOURCES.items()],'clips':metrics,'humanAuditionVerified':False,'physicalPhoneAuditionVerified':False}
(ROOT/'TestResults/natural-contact-v10.json').write_text(json.dumps(report,indent=2)+'\n')
credits=ROOT/'Documentation/References/V10/AUDIO_CREDITS.md'
credits.write_text('# Natural contact audio v10\n\nAll five source recordings are CC0 1.0 (https://creativecommons.org/publicdomain/zero/1.0/). Downloaded public high-quality previews from Freesound.\n\n'+'\n'.join(f'- [{d} — {a}](https://freesound.org/people/{a}/sounds/{i}/)' for n,(a,i,d) in SOURCES.items())+'\n\n'+report['method']+'\n\nSource hashes, exact clip metrics and preparation script are retained. No other game audio is included.\n')
print(f'Baked {len(metrics)} licensed recording-based clips to {DEST}')
