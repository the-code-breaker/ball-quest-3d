"""Read-only geometric preflight for the explicitly authored v9 route recipe source."""
import json
import math
from pathlib import Path
import re

ROOT=Path(__file__).resolve().parents[1]
SOURCE=(ROOT/'Assets/_BallQuest/Scripts/Editor/CrazyCampaignGenerator.cs').read_text()
FAMILIES=['Ribbon','Knife','Orbit','Switchbacks','Figure','Islands','Clockwork','Catwalk','Maze','Velocity','Finale']

def add(a,b):return tuple(x+y for x,y in zip(a,b))
def mul(a,n):return tuple(x*n for x in a)
def norm(a):return mul(a,1/math.sqrt(sum(x*x for x in a)))
def lerp(a,b,t):return add(mul(a,1-t),mul(b,t))
def length(a):return math.sqrt(sum(x*x for x in a))
def sub(a,b):return add(a,mul(b,-1))

class Route:
    def __init__(self,y):self.last=(0,y,0);self.forward=(0,0,1);self.points=[self.last];self.widths=[];self.rails=[];self.drops=0;self.length=0
    @property
    def right(self):return (self.forward[2],0,-self.forward[0])
    def To(self,p,width=4.4,rails=True,style=None,optional=False):
        a=self.last;delta=sub(p,a);count=max(1,math.ceil(length(delta)/3))
        for i in range(1,count+1):
            q=lerp(a,p,i/count)
            if not optional:self.length+=length(sub(q,self.points[-1]));self.points.append(q);self.widths.append(width);self.rails.append(rails)
        self.last=p
        if math.hypot(delta[0],delta[2])>.001:self.forward=norm((delta[0],0,delta[2]))
    def Run(self,distance,width=4.4,rise=0,rails=True,style=None):self.To(add(add(self.last,mul(self.forward,distance)),(0,rise,0)),width,rails,style)
    def Turn(self,angle,radius=10,width=4.4,rise=0,rails=True,style=None):
        start=self.last;heading=self.forward;right=self.right;sign=1 if angle>=0 else -1;count=max(2,math.ceil(abs(angle)/7))
        for i in range(1,count+1):
            t=i/count;a=abs(angle)*math.pi/180*t
            self.To(add(add(add(start,mul(right,sign*radius*(1-math.cos(a)))),mul(heading,radius*math.sin(a))),(0,rise*t,0)),width,rails,style)
        a=angle*math.pi/180;self.forward=(heading[0]*math.cos(a)+heading[2]*math.sin(a),0,heading[2]*math.cos(a)-heading[0]*math.sin(a))
    def Bend(self,lateral,distance,width=4.4,rise=0,rails=True,style=None):
        start=self.last;heading=self.forward;right=self.right;count=max(6,math.ceil(distance/3))
        for i in range(1,count+1):
            t=i/count;s=t*t*(3-2*t);self.To(add(add(add(start,mul(heading,distance*t)),mul(right,lateral*s)),(0,rise*t,0)),width,rails,style)
        self.forward=heading
    def Checkpoint(self):self.Run(7,6)
    def Beam(self,distance,width):self.Run(3,4);self.Run(distance,width,0,False);self.Run(4,4)
    def Drop(self,height,catchLength=38,catchWidth=7):
        self.Run(7,4.6,0,False);self.last=add(self.last,(0,-height,0));self.points.append(self.last);self.widths.append(max(8,catchWidth));self.rails.append(False);self.Run(catchLength,max(8,catchWidth));self.drops+=1
    def Boost(self,distance=40):self.Run(distance,5.8);self.Run(10,6.3)
    def Door(self,phase=0):self.Run(20,5.2)
    def Hazard(self,kind,side=1,width=6.4,phase=0):self.Run(24,width)
    def Fragile(self,width=3.2):self.Run(4,4.4);self.Run(6,width);self.Run(7,4.5)
    def Shuttle(self,width=3.4):self.Run(4,4.8);self.Run(6,width);self.Run(7,4.8)
    def Fork(self,distance,offset,width=3.6):self.Bend(-offset,distance*.5,width,1.2);self.Bend(offset,distance*.5,width,-1.2)
    def Eight(self,x,z,turns=1,rise=10,width=3.8):
        start=self.last;heading=self.forward;right=self.right;count=math.ceil(turns*80)
        for i in range(1,count+1):
            t=i/count;a=t*turns*math.pi*2;self.To(add(add(add(start,mul(right,x*math.sin(a))),mul(heading,z*math.sin(a*2))),(0,rise*t,0)),width)
    def SideReward(self,side):pass

def build(family,variant):
    section=SOURCE.split('static void '+family+'(')[1].split('\n        static ')[0]
    label=f'case {variant}:' if variant<4 else 'default:'
    recipe=section.split(label)[1].split('break;')[0]
    r=Route(52+variant*2);r.Run(10,6.2)
    for method,args in re.findall(r'r\.(\w+)\((.*?)\);',recipe):
        values=[]
        for arg in args.split(','):
            arg=arg.strip().replace('true','True').replace('false','False')
            if not arg:continue
            if arg.startswith(('TrackStyle.','ObstacleKind.')):values.append(arg)
            else:values.append(float(arg.rstrip('f')) if arg not in ('True','False') else arg=='True')
        getattr(r,method)(*values)
    r.Run(12,5.8);return r

def stats(r):
    xs=[p[0] for p in r.points];zs=[p[2] for p in r.points];ys=[p[1] for p in r.points]
    return dict(length=round(r.length,2),segments=len(r.widths),minimumWidth=min(r.widths),unrailedSegments=sum(not x for x in r.rails),bounds=[round(max(xs)-min(xs),1),round(max(ys)-min(ys),1),round(max(zs)-min(zs),1)],drops=r.drops)

def crossings(r):
    results=[]
    for i in range(len(r.points)-1):
        a,b=r.points[i:i+2]
        for j in range(i+9,len(r.points)-1):
            c,d=r.points[j:j+2];ab=(b[0]-a[0],b[2]-a[2]);cd=(d[0]-c[0],d[2]-c[2]);offset=(c[0]-a[0],c[2]-a[2])
            denominator=ab[0]*cd[1]-ab[1]*cd[0]
            if abs(denominator)<.00001:continue
            t=(offset[0]*cd[1]-offset[1]*cd[0])/denominator;u=(offset[0]*ab[1]-offset[1]*ab[0])/denominator
            if 0<=t<=1 and 0<=u<=1:
                vertical=abs((a[1]+(b[1]-a[1])*t)-(c[1]+(d[1]-c[1])*u))
                if vertical<4:results.append(dict(segmentA=i,segmentB=j,clearance=round(vertical,2)))
    return results

if __name__=='__main__':
    records=[]
    for v in range(5):
        for f,name in enumerate(FAMILIES):
            route=build(name,v);records.append(dict(id=v*11+f+1,family=name,**stats(route),lowCrossings=crossings(route)))
    print(json.dumps(records,indent=2))
