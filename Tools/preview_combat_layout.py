"""Static layout illustration using the shipped UI sprites; not a Unity gameplay capture."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import math
ROOT=Path(__file__).resolve().parents[1];UI=ROOT/'Assets/Resources/UI'
FONT=ROOT/'Assets/Resources/Fonts/LilitaOne-Regular.ttf'
im=Image.new('RGBA',(1280,720),(19,39,33,255));draw=ImageDraw.Draw(im)
def text(label,x,y,size=20,color=(239,234,211)):
 draw.text((x,y),label,font=ImageFont.truetype(str(FONT),size),fill=color,anchor='mm')
def sprite(name,x,y,size):
 art=Image.open(UI/(name+'.png')).convert('RGBA').resize((round(size),round(size)),Image.Resampling.LANCZOS)
 im.alpha_composite(art,(round(x-size/2),round(720-y-size/2)))
def circle(name,label,x,y,size):
 sprite('stick',x,y,size);sprite(name,x,y+size*.1,size*.52)
 text(label,x,720-(y-size*.28),22 if size>150 else 14)
def card(label,x,y,w,h):
 draw.rounded_rectangle((x-w/2,720-y-h/2,x+w/2,720-y+h/2),radius=16,fill=(30,53,44),outline=(166,132,71),width=2)
 text(label,x,720-y,18)
card('PLAYER STATS',125,610,230,200)
card('CAMP',1155,685,210,48)
card('ORBS / DRAGON',1155,560,230,190)
card('OFFER ORBS',1155,435,210,48)
text('CONTROL LAYOUT PREVIEW',640,165,27,(230,193,112))
text('Four skills + Dodge around Attack',640,204,20)
text('Existing artwork and game actions',640,235,17,(164,186,173))
sprite('stick',135,135,190);sprite('thumb',135,135,65);text('Drag to ride',135,690,19)
circle('attack','ATTACK',1280-12-120,12+155,180)
for i,(icon,label) in enumerate([('whirlwind','Whirlwind'),('guard','Iron Guard'),('charge','Charge'),('kick','Rear Kick'),('dodge','DODGE')]):
 a=math.radians(75+35*i);circle(icon,label,1280-12-120+math.cos(a)*175,12+155+math.sin(a)*175,92)
circle('heal','HEAL',640,78,100);text('Swipe open right side to look',640,708,14)
im.convert('RGB').save(ROOT/'Docs/mobile-control-layout.png',optimize=True)
print('Saved Docs/mobile-control-layout.png (layout illustration).')
