"""Drive normal native touch input; QA snapshots are read-only observations."""
import json, os, pathlib, subprocess, time
out = pathlib.Path('screenshots'); out.mkdir(exist_ok=True)
udid = os.environ['UDID']; bundle = os.environ['APP_BUNDLE_ID']
container = pathlib.Path(subprocess.check_output(['xcrun','simctl','get_app_container',udid,bundle,'data'],text=True).strip())
seq = 0
import importlib.util
spec=importlib.util.spec_from_file_location('touchbridge',pathlib.Path(__file__).with_name('native-touch-bridge.py'))
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
bridge=module.TouchBridge(out,udid,bundle)

def state():
    global container
    deadline = time.time()+180
    while time.time()<deadline:
        for p in container.rglob('qa-state.json'):
            try:
                s=json.loads(p.read_text())
                assert not s['errors'],s['errors']
                if s["frame"] >= 30: return s
            except (json.JSONDecodeError,FileNotFoundError): pass
        # Simulator can relocate the data container during a slow first launch.
        try:
            container = pathlib.Path(subprocess.check_output(['xcrun','simctl','get_app_container',udid,bundle,'data'],text=True).strip())
        except subprocess.CalledProcessError: pass
        time.sleep(1)
    (out/'container-probe.txt').write_text(str(container)+'\n'+'\n'.join(str(p) for p in container.rglob('*') if p.is_file()))
    probe_errors=list(container.rglob('qa-recorder-error.txt'))
    if probe_errors:
        detail=probe_errors[0].read_text()
        (out/'qa-recorder-error.txt').write_text(detail)
        raise AssertionError('Native diagnostic recorder failed: '+detail)
    raise AssertionError('Read-only native snapshot missing')

def capture(label):
    (out/(label+'.json')).write_text(json.dumps(state(),indent=2))
    subprocess.run(['xcrun','simctl','io',udid,'screenshot',str(out/(label+'.png'))],check=True)

def point(item):
    assert 0<item['x']<1 and 0<item['y']<1,item
    return f"{round(item['x']*100)}%, {round(item['y']*100)}%"

def swipe_point(item):
    return f"{item['x']*100:.4f}%, {item['y']*100:.4f}%"

def action(body):
    global seq
    seq+=1
    p=out/f'input-{seq:03}.yaml'
    p.write_text(f'appId: {bundle}\n---\n'+body+'\n')
    if bridge.proc is not None and body.startswith("- tapOn:"):
        bridge.tap_direct(body.split('"')[1])
    else: bridge.action(body)
    time.sleep(.1)

def tap(text):
    choices=[i for i in state()['items'] if i['kind']=='button' and i['interactable'] and text.casefold() in (i.get('text','').strip().casefold(),i['name'].strip().casefold())]
    assert choices,('Button missing',text)
    action('- tapOn:\n    point: "'+point(choices[0])+'"')

def val(key):return next(v['value'] for v in state()['values'] if v['key']==key)

def number(key): return float(val('EmpireIdleGame.'+key))
def press(x,y):
    s=state(); factor=min(s['safeW']/430,s['safeH']/932)
    left=s['safeX']+(s['safeW']-430*factor)/2
    top=s['height']-s['safeY']-s['safeH']+(s['safeH']-932*factor)/2
    action('- tapOn:\n    point: "'+f"{100*(left+x*factor)/s['width']:.4f}%, {100*(top+y*factor)/s['height']:.4f}%"+'"')
def restart():
    subprocess.run(['xcrun','simctl','terminate',udid,bundle],check=True)
    for p in container.rglob('qa-state.json'):p.unlink()
    subprocess.run(['xcrun','simctl','launch',udid,bundle],check=True)
    time.sleep(4)
state();action('- tapOn:\n    point: "95%, 95%"');capture('01-start')
c=number('coins');time.sleep(3);assert number('coins')>c,'No idle income'
w=number('workers');press(215,341);assert number('workers')==w+1,'Worker purchase failed'
for _ in range(18):press(215,636)
f=number('factories');press(215,521);assert number('factories')==f+1,'Factory purchase failed'
capture('02-upgrades')
w=number('workers');f=number('factories');c=number('coins');restart()
assert number('workers')==w and number('factories')==f and number('coins')>=c-1,'Purchase/currency persistence failed'
capture('03-restored')
# Earn through real collect touches, never write currency/model state.
while number('coins')<2500:press(215,636)
capture('04-earned-expansion')
p=number('prestige')
for attempt in range(3):
    press(215,795)
    deadline=time.time()+8
    while number('prestige')==p and time.time()<deadline:time.sleep(.4)
    if number('prestige')!=p:break
capture('04b-expansion-result')
assert number('prestige')==p+1 and number('workers')==1 and number('factories')==1,'Prestige reset failed'
restart();assert number('prestige')==p+1,'Prestige persistence failed'
capture('05-expanded-restored')
(out/'result.json').write_text(json.dumps({'idleIncome':True,'workerPurchase':True,'factoryPurchase':True,'collect':True,'purchaseCurrencyColdLaunch':True,'earnedPrestige':True,'prestigeColdLaunch':True}))
