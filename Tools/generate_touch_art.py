"""Original leather/brass control textures and sky lookup texture. Requires Pillow/numpy."""
from PIL import Image,ImageDraw,ImageFilter
import numpy as np,random
from pathlib import Path
root=Path(__file__).resolve().parents[1];out=root/'Assets/Resources/UI';out.mkdir(exist_ok=True)
rng=random.Random(7419)
for name in ['button','stick','thumb']:
 n=256;im=Image.new('RGBA',(n,n));d=ImageDraw.Draw(im)
 shape=d.rounded_rectangle if name=='button' else d.ellipse
 def draw(box,fill,width=0,outline=None):
  if name=='button':shape(box,radius=30,fill=fill,outline=outline,width=width)
  else:shape(box,fill=fill,outline=outline,width=width)
 draw((3,3,252,252),(25,22,17,240));draw((7,7,248,248),(164,128,66,255));draw((13,13,242,242),(70,52,32,255));draw((20,20,235,235),(34,48,44,235))
 for _ in range(2200):
  x,y=rng.randrange(24,232),rng.randrange(24,232)
  if name=='button' or (x-128)**2+(y-128)**2<102**2:d.point((x,y),fill=(85,91,66,rng.randrange(30,75)))
 if name=='button':
  for edge in [27,228]:
   for i in range(36,223,13):d.line((i,edge,i+5,edge),fill=(176,155,104,160),width=2)
 else:
  d.ellipse((38,38,217,217),outline=(181,161,110,180),width=2)
  for a in [0,90,180,270]:
   import math
   x,y=128+83*math.cos(math.radians(a)),128+83*math.sin(math.radians(a));d.ellipse((x-3,y-3,x+3,y+3),fill=(217,193,130,220))
 if name=='thumb':d.ellipse((73,73,182,182),fill=(90,106,83,255),outline=(225,210,159,255),width=4)
 im.save(out/(name+'.png'))
for name in ['attack','dodge','heal','whirlwind','guard','charge','kick']:
 im=Image.new('RGBA',(128,128));d=ImageDraw.Draw(im);gold=(245,222,157,255);pale=(223,241,230,255)
 if name=='attack':
  d.polygon([(35,96),(29,89),(86,22),(108,14),(105,37)],fill=pale);d.line((25,70,58,99),fill=gold,width=9);d.line((22,106,39,88),fill=gold,width=11)
 elif name=='dodge':
  for x in [24,58]:d.line((x,32,x+28,64,x,96),fill=gold,width=11)
 elif name=='heal':
  d.rounded_rectangle((36,40,92,107),radius=17,fill=(111,193,146,255),outline=gold,width=4);d.rectangle((49,22,79,43),fill=gold);d.rectangle((57,56,71,91),fill=pale);d.rectangle((45,67,83,80),fill=pale)
 elif name=='whirlwind':
  d.arc((16,16,112,112),20,305,fill=pale,width=8);d.arc((34,34,94,94),190,490,fill=gold,width=8);d.polygon([(96,12),(112,41),(82,34)],fill=pale)
 elif name=='guard':
  d.polygon([(64,15),(106,31),(98,81),(64,111),(30,81),(22,31)],fill=(94,152,186,255),outline=gold,width=6);d.line((64,28,64,94),fill=pale,width=7)
 elif name=='charge':
  d.polygon([(75,12),(30,68),(59,68),(46,114),(103,53),(75,53)],fill=gold)
 else:
  d.arc((25,16,103,106),0,180,fill=gold,width=16);d.line((25,62,25,31),fill=gold,width=16);d.line((103,62,103,31),fill=gold,width=16)
  for x in [25,103]:
   for y in [40,59]:d.ellipse((x-3,y-3,x+3,y+3),fill=(47,53,43,255))
 im.save(out/(name+'.png'))
# Periodic noise and stars share one RGBA lookup; no per-frame CPU texture generation.
n=512;h=256;nr=np.random.default_rng(7419);field=np.zeros((h,n),dtype=np.float32)
for step,weight in [(16,.55),(32,.25),(64,.13),(128,.07)]:
 small=Image.fromarray(nr.integers(0,256,(step//2,step),dtype=np.uint8));big=np.array(small.resize((n,h),Image.Resampling.BICUBIC),dtype=np.float32)/255;field+=big*weight
# Blend opposing edges for wrap-safe lookup.
for x in range(12):
 mix=x/12;v=(field[:,x]+field[:,-1-x])/2;field[:,x]=v*(1-mix)+field[:,x]*mix;field[:,-1-x]=v*(1-mix)+field[:,-1-x]*mix
stars=np.zeros((h,n),dtype=np.uint8)
for _ in range(650):stars[rng.randrange(0,h),rng.randrange(n)]=rng.randrange(130,256)
rgba=np.zeros((h,n,4),dtype=np.uint8);rgba[:,:,0]=(field*255).astype(np.uint8);rgba[:,:,1]=stars;rgba[:,:,3]=255
Image.fromarray(rgba).save(out/'sky-lookup.png')
print('Generated 10 touch sprites and sky lookup.')
