"""Generate original, deterministic game audio; no external sound recordings.
Requires numpy. Run from any directory. 22.05kHz mono PCM, with loop fades.
"""
from pathlib import Path
import numpy as np,wave
out=Path(__file__).resolve().parents[1]/'Assets/Resources/Audio';out.mkdir(parents=True,exist_ok=True)
sr=22050;rng=np.random.default_rng(7419)
def save(name,samples,gain=.75):
    peak=max(.001,float(np.max(np.abs(samples))));samples=samples/peak*gain
    fade=min(220,len(samples)//8);samples[:fade]*=np.linspace(0,1,fade);samples[-fade:]*=np.linspace(1,0,fade)
    with wave.open(str(out/(name+'.wav')),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(sr);f.writeframes((samples*32767).astype('<i2').tobytes())
def tone(duration,freq,decay=8):
    t=np.arange(int(duration*sr))/sr;phase=2*np.pi*freq*t
    return (np.sin(phase)+.22*np.sin(phase*2))*np.exp(-decay*t)
def noise(duration,decay=10):
    t=np.arange(int(duration*sr))/sr;return rng.uniform(-1,1,len(t))*np.exp(-decay*t)
for name,duration,frequency in [('hit',.22,130),('hoof',.13,95),('kick',.25,85),('guard',.65,440),('charge',.5,180),('death',1.1,90),('ui',.09,680),('roar',1.8,65),('dive',.7,85)]:
    a=tone(duration,frequency,3 if duration>.5 else 15);a+=noise(duration,5)*(.45 if name in ['hit','hoof','kick','roar','dive'] else .04);save(name,a)
for name in ['swing','whirlwind','fireball']:
    duration=.32 if name=='swing' else .7;t=np.arange(int(duration*sr))/sr
    n=rng.normal(0,.3,len(t));n=np.convolve(n,np.ones(7)/7,mode='same')
    a=n*np.sin(np.pi*t/duration)**2+np.sin(2*np.pi*(320*t-150*t*t))*np.exp(-8*t)*.05
    save(name,a)
for name,notes in [('gem',[660,880,1100]),('loot',[440,660]),('heal',[330,440,660]),('level',[392,494,587,784])]:
    a=np.zeros(int(sr*1.3))
    for i,f in enumerate(notes):
        note=tone(.7,f,6);offset=int(i*.12*sr);a[offset:offset+len(note)]+=note
    save(name,a)
for night in [False,True]:
    duration=12;n=int(duration*sr);t=np.arange(n)/sr
    wind=rng.normal(0,.2,n);wind=np.convolve(wind,np.ones(120)/120,mode='same')
    a=wind*(.6+.25*np.sin(2*np.pi*t/duration))
    for start in ([1.5,4.8,8.2] if not night else [2.3,6.4,9.8]):
        length=.35;v=np.arange(int(length*sr))/sr
        call=np.sin(2*np.pi*((2000 if not night else 3300)*v+140*v*v))*np.sin(np.pi*v/length)**2*.025
        offset=int(start*sr);a[offset:offset+len(call)]+=call
    save('ambience-night' if night else 'ambience-day',a,.5)
# Original sparse pentatonic pad/arpeggio, intentionally quiet in the mix.
a=np.zeros(16*sr)
for i,f in enumerate([196,246.94,293.66,369.99,293.66,246.94,220,293.66]):
    t=np.arange(2*sr)/sr;note=(np.sin(2*np.pi*f*t)+.3*np.sin(2*np.pi*f/2*t))*np.sin(np.pi*t/2)**2*.18
    a[i*2*sr:(i+1)*2*sr]+=note
save('music',a,.45)
print('Generated',len(list(out.glob('*.wav'))),'original audio files')
