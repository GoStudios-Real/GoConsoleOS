using System;

namespace GoConsoleOS.GoConsole.Views;

internal static class GameFiles
{
    public static string Snake => @"<!DOCTYPE html><html><head><meta charset=""UTF-8""><style>
*{margin:0;padding:0;box-sizing:border-box}
body{background:#0D0D14;display:flex;justify-content:center;align-items:center;height:100vh;overflow:hidden;font-family:'Segoe UI',sans-serif}
#wrap{text-align:center}
#score{color:#0066FF;font-size:22px;font-weight:bold;margin-bottom:10px}
canvas{border:2px solid #0066FF;border-radius:4px;background:#1A1A2E}
#overlay{position:absolute;top:0;left:0;width:100%;height:100%;display:flex;justify-content:center;align-items:center;background:rgba(13,13,20,0.8);cursor:pointer}
#overlay span{color:#0066FF;font-size:28px;font-weight:bold}
#info{color:#8888AA;font-size:12px;margin-top:8px}
</style></head><body>
<div id=""wrap"">
<div id=""score"">0</div>
<div style=""position:relative;display:inline-block"">
<canvas id=""c"" width=""400"" height=""400""></canvas>
<div id=""overlay""><span>▶ PLAY</span></div>
</div>
<div id=""info"">Arrow keys to move &bull; R to restart</div>
</div>
<script>
const c=document.getElementById('c'),ctx=c.getContext('2d'),score=document.getElementById('score'),overlay=document.getElementById('overlay');
const S=20,W=20,H=20;let dir={x:1,y:0},nextDir={x:1,y:0},snake,food,alive,run,tick=0;
function init(){snake=[{x:10,y:10},{x:9,y:10},{x:8,y:10}];dir={x:1,y:0};nextDir={x:1,y:0};alive=true;tick=0;spawnFood();score.textContent='0'}
function spawnFood(){let f;do{f={x:Math.floor(Math.random()*W),y:Math.floor(Math.random()*H)}}while(snake.some(s=>s.x===f.x&&s.y===f.y));food=f}
function update(){if(!alive)return;tick++;if(tick<5)return;tick=0;dir={...nextDir};const head={x:snake[0].x+dir.x,y:snake[0].y+dir.y};
if(head.x<0||head.x>=W||head.y<0||head.y>=H||snake.some(s=>s.x===head.x&&s.y===head.y)){alive=false;return}
snake.unshift(head);if(head.x===food.x&&head.y===food.y){score.textContent=parseInt(score.textContent)+10;spawnFood()}else{snake.pop()}}
function draw(){ctx.fillStyle='#1A1A2E';ctx.fillRect(0,0,c.width,c.height);
snake.forEach((s,i)=>{ctx.fillStyle=i===0?'#0066FF':'#0066FF66';ctx.fillRect(s.x*S,s.y*S,S-1,S-1)});
ctx.fillStyle='#FF4D8C';ctx.fillRect(food.x*S,food.y*S,S-1,S-1);
if(!alive){ctx.fillStyle='rgba(13,13,20,0.7)';ctx.fillRect(0,0,c.width,c.height);ctx.fillStyle='#FF5252';ctx.font='24px sans-serif';ctx.textAlign='center';ctx.fillText('GAME OVER',200,200);ctx.fillStyle='#8888AA';ctx.font='14px sans-serif';ctx.fillText('Click to restart',200,230)}}
function loop(){update();draw();requestAnimationFrame(loop)}
document.addEventListener('keydown',e=>{const k=e.key;if(k==='ArrowUp'&&dir.y!==1)nextDir={x:0,y:-1};else if(k==='ArrowDown'&&dir.y!==-1)nextDir={x:0,y:1};else if(k==='ArrowLeft'&&dir.x!==1)nextDir={x:-1,y:0};else if(k==='ArrowRight'&&dir.x!==-1)nextDir={x:1,y:0};else if(k==='r'||k==='R'){init()}});
overlay.addEventListener('click',()=>{overlay.style.display='none';init()});
loop();
</script></body></html>";

    public static string Pong => @"<!DOCTYPE html><html><head><meta charset=""UTF-8""><style>
*{margin:0;padding:0;box-sizing:border-box}
body{background:#0D0D14;display:flex;justify-content:center;align-items:center;height:100vh;overflow:hidden;font-family:'Segoe UI',sans-serif}
#wrap{text-align:center}
#score{color:#0066FF;font-size:22px;font-weight:bold;margin-bottom:10px}
canvas{border:2px solid #0066FF;border-radius:4px;background:#1A1A2E}
#overlay{position:absolute;top:0;left:0;width:100%;height:100%;display:flex;justify-content:center;align-items:center;background:rgba(13,13,20,0.8);cursor:pointer}
#overlay span{color:#0066FF;font-size:28px;font-weight:bold}
#info{color:#8888AA;font-size:12px;margin-top:8px}
</style></head><body>
<div id=""wrap"">
<div id=""score"">0 - 0</div>
<div style=""position:relative;display:inline-block"">
<canvas id=""c"" width=""600"" height=""400""></canvas>
<div id=""overlay""><span>▶ PLAY</span></div>
</div>
<div id=""info"">Mouse to move paddle &bull; R to restart</div>
</div>
<script>
const c=document.getElementById('c'),ctx=c.getContext('2d'),score=document.getElementById('score'),overlay=document.getElementById('overlay');
const W=600,H=400,PW=10,PH=60,BS=8;let px=200,py=H/2-PH/2,bx=W/2,by=H/2,bvx=4,bvy=3,ps=0,as=0,run=0;
function reset(){bx=W/2;by=H/2;bvx=4*(Math.random()>.5?1:-1);bvy=3*(Math.random()>.5?1:-1)}
function update(){if(!run)return;bx+=bvx;by+=bvy;
if(by<=0||by>=H-BS)bvy=-bvy;
if(bx<=PW&&by+BS>py&&by<py+PH){bvx=-bvx;bx=PW+1;bvy+=(Math.random()-.5)*2}
if(bx>=W-PW-BS&&by+BS>ay&&by<ay+PH){bvx=-bvx;bx=W-PW-BS-1;bvy+=(Math.random()-.5)*2}
if(bx<0){as++;reset()}
if(bx>W-BS){ps++;reset()}
score.textContent=ps+' - '+as;
const target=by-ay-PH/2+PH/2;ay+=Math.sign(target)*2.5}
let ay=H/2-PH/2;
function draw(){ctx.fillStyle='#1A1A2E';ctx.fillRect(0,0,W,H);
ctx.fillStyle='#FFFFFF';ctx.fillRect(10,py,PW,PH);ctx.fillRect(W-PW-10,ay,PW,PH);
ctx.fillStyle='#0066FF';ctx.fillRect(bx,by,BS,BS);
ctx.fillStyle='#333355';ctx.beginPath();ctx.setLineDash([10,10]);ctx.moveTo(W/2,0);ctx.lineTo(W/2,H);ctx.stroke()}
function loop(){update();draw();requestAnimationFrame(loop)}
document.addEventListener('mousemove',e=>{const r=c.getBoundingClientRect();py=e.clientY-r.top-PH/2;if(py<0)py=0;if(py>H-PH)py=H-PH});
document.addEventListener('keydown',e=>{if(e.key==='r'||e.key==='R'){ps=0;as=0;reset()}});
overlay.addEventListener('click',()=>{overlay.style.display='none';run=1});
loop();
</script></body></html>";

    public static string Breakout => @"<!DOCTYPE html><html><head><meta charset=""UTF-8""><style>
*{margin:0;padding:0;box-sizing:border-box}
body{background:#0D0D14;display:flex;justify-content:center;align-items:center;height:100vh;overflow:hidden;font-family:'Segoe UI',sans-serif}
#wrap{text-align:center}
#score{color:#0066FF;font-size:22px;font-weight:bold;margin-bottom:10px}
canvas{border:2px solid #0066FF;border-radius:4px;background:#1A1A2E}
#overlay{position:absolute;top:0;left:0;width:100%;height:100%;display:flex;justify-content:center;align-items:center;background:rgba(13,13,20,0.8);cursor:pointer}
#overlay span{color:#0066FF;font-size:28px;font-weight:bold}
#info{color:#8888AA;font-size:12px;margin-top:8px}
</style></head><body>
<div id=""wrap"">
<div id=""score"">0</div>
<div style=""position:relative;display:inline-block"">
<canvas id=""c"" width=""500"" height=""400""></canvas>
<div id=""overlay""><span>▶ PLAY</span></div>
</div>
<div id=""info"">Mouse to move paddle &bull; R to restart</div>
</div>
<script>
const c=document.getElementById('c'),ctx=c.getContext('2d'),score=document.getElementById('score'),overlay=document.getElementById('overlay');
const W=500,H=400,PW=80,PH=10,BR=25,BC=8;let px=W/2-PW/2,bx=W/2,by=300,bvx=4,bvy=-4,pts=0,run=0,lives=3;
let bricks=[];for(let r=0;r<5;r++)for(let c=0;c<BC;c++)bricks.push({x:c*(BR+4)+12,y:r*18+30,w:BR,h:14,alive:true});
function reset(){bx=W/2;by=300;bvx=4*(Math.random()>.5?1:-1);bvy=-4}
function update(){if(!run)return;
bx+=bvx;by+=bvy;
if(bx<=0||bx>=W-8)bvx=-bvx;
if(by<=0)bvy=-bvy;
if(by>=H){lives--;if(lives>0)reset();else{run=0;return}}
if(by+8>=380&&bx+8>px&&bx<px+PW){bvy=-Math.abs(bvy);by=379}
bricks.forEach(b=>{if(!b.alive)return;if(bx+8>b.x&&bx<b.x+b.w&&by+8>b.y&&by<b.y+b.h){b.alive=false;bvy=-bvy;pts+=10;score.textContent=pts}});
if(bricks.every(b=>!b.alive)){run=0;score.textContent='WIN!'}}
function draw(){ctx.fillStyle='#1A1A2E';ctx.fillRect(0,0,W,H);
ctx.fillStyle='#0066FF';ctx.fillRect(px,380,PW,PH);
bricks.forEach(b=>{if(!b.alive)return;ctx.fillStyle=['#FF4D8C','#FFD600','#0066FF','#7B2DFF','#00E676'][Math.floor(b.y/18)%5];ctx.fillRect(b.x,b.y,b.w,b.h)});
ctx.fillStyle='#FFFFFF';ctx.fillRect(bx,by,8,8);
for(let i=0;i<lives;i++){ctx.fillStyle='#FF5252';ctx.fillRect(W-20-i*20,10,11,11)}
if(!run&&lives<=0){ctx.fillStyle='rgba(13,13,20,0.7)';ctx.fillRect(0,0,W,H);ctx.fillStyle='#FF5252';ctx.font='24px sans-serif';ctx.textAlign='center';ctx.fillText('GAME OVER',250,200);ctx.fillStyle='#8888AA';ctx.font='14px sans-serif';ctx.fillText('Click to restart',250,230)}}
function loop(){update();draw();requestAnimationFrame(loop)}
document.addEventListener('mousemove',e=>{const r=c.getBoundingClientRect();px=e.clientX-r.left-PW/2;if(px<0)px=0;if(px>W-PW)px=W-PW});
document.addEventListener('keydown',e=>{if(e.key==='r'||e.key==='R'){pts=0;lives=3;bricks.forEach(b=>b.alive=true);reset()}});
overlay.addEventListener('click',()=>{overlay.style.display='none';pts=0;lives=3;bricks.forEach(b=>b.alive=true);reset();run=1});
loop();
</script></body></html>";

    public static string Tetris => @"<!DOCTYPE html><html><head><meta charset=""UTF-8""><style>
*{margin:0;padding:0;box-sizing:border-box}
body{background:#0D0D14;display:flex;justify-content:center;align-items:center;height:100vh;overflow:hidden;font-family:'Segoe UI',sans-serif}
#wrap{text-align:center}
#score{color:#0066FF;font-size:22px;font-weight:bold;margin-bottom:10px}
canvas{border:2px solid #0066FF;border-radius:4px;background:#1A1A2E}
#overlay{position:absolute;top:0;left:0;width:100%;height:100%;display:flex;justify-content:center;align-items:center;background:rgba(13,13,20,0.8);cursor:pointer}
#overlay span{color:#0066FF;font-size:28px;font-weight:bold}
#next{color:#8888AA;font-size:12px;margin-bottom:4px}
#info{color:#8888AA;font-size:12px;margin-top:8px}
</style></head><body>
<div id=""wrap"">
<div id=""score"">0</div>
<div id=""next"">Next piece</div>
<div style=""position:relative;display:inline-block"">
<canvas id=""c"" width=""240"" height=""480""></canvas>
<canvas id=""n"" width=""120"" height=""120"" style=""position:absolute;top:0;right:-130px;border:1px solid #333355;border-radius:4px;background:#1A1A2E""></canvas>
<div id=""overlay""><span>▶ PLAY</span></div>
</div>
<div id=""info"">← → ↓ to move &bull; ↑ to rotate &bull; Space to drop &bull; R restart</div>
</div>
<script>
const c=document.getElementById('c'),ctx=c.getContext('2d'),n=document.getElementById('n'),nctx=n.getContext('2d'),score=document.getElementById('score'),overlay=document.getElementById('overlay');
const COLS=10,ROWS=20,BS=24;let grid=[],cur,nxt,pos,pts=0,run=0,tick=0,gameOver=0;
const PIECES=[[[1,1,1,1]],[[1,1],[1,1]],[[1,0],[1,1],[0,1]],[[0,1],[1,1],[1,0]],[[1,1,0],[0,1,1]],[[0,1,1],[1,1,0]],[[1,1,1],[0,1,0]]];
const COLORS=['#0066FF','#FFD600','#7B2DFF','#FF4D8C','#00E676','#FF8C00','#E040FB'];
function initGrid(){grid=Array.from({length:ROWS},()=>Array(COLS).fill(0))}
function newPiece(){cur=nxt||PIECES[Math.floor(Math.random()*7)];pos={x:Math.floor((COLS-cur[0].length)/2),y:0};nxt=PIECES[Math.floor(Math.random()*7)];if(collides()){gameOver=1;run=0}}
function collides(p,o){p=p||cur;o=o||pos;for(let r=0;r<p.length;r++)for(let c=0;c<p[r].length;c++)if(p[r][c]){const nx=o.x+c,ny=o.y+r;if(nx<0||nx>=COLS||ny>=ROWS||(ny>=0&&grid[ny][nx]))return true}return false}
function lock(){for(let r=0;r<cur.length;r++)for(let c=0;c<cur[r].length;c++)if(cur[r][c]){const y=pos.y+r;if(y>=0)grid[y][pos.x+c]=COLORS.indexOf(COLORS[(pts/100)%7])+1}
let cleared=0;for(let r=ROWS-1;r>=0;r--){if(grid[r].every(v=>v!==0)){grid.splice(r,1);grid.unshift(Array(COLS).fill(0));cleared++;r++}}
if(cleared)pts+=cleared*100;score.textContent=pts;newPiece()}
function update(){if(!run||gameOver)return;tick++;if(tick<10)return;tick=0;pos.y++;if(collides()){pos.y--;lock()}}
function move(dx){pos.x+=dx;if(collides())pos.x-=dx}
function rotate(){const rot=cur[0].map((_,i)=>cur.map(r=>r[i]).reverse());if(!collides(rot))cur=rot}
function drop(){while(!collides())pos.y++;pos.y--;lock()}
function draw(){ctx.fillStyle='#1A1A2E';ctx.fillRect(0,0,240,480);
for(let r=0;r<ROWS;r++)for(let c=0;c<COLS;c++){if(grid[r][c]){ctx.fillStyle=COLORS[grid[r][c]-1];ctx.fillRect(c*BS,r*BS,BS-1,BS-1)}}
if(!gameOver){const p=cur;for(let r=0;r<p.length;r++)for(let c=0;c<p[r].length;c++)if(p[r][c]){ctx.fillStyle=COLORS[(pts/100)%7];ctx.fillRect((pos.x+c)*BS,(pos.y+r)*BS,BS-1,BS-1)}}
if(gameOver){ctx.fillStyle='rgba(13,13,20,0.7)';ctx.fillRect(0,0,240,480);ctx.fillStyle='#FF5252';ctx.font='18px sans-serif';ctx.textAlign='center';ctx.fillText('GAME OVER',120,240);ctx.fillStyle='#8888AA';ctx.font='12px sans-serif';ctx.fillText('Click to restart',120,260)}
nctx.fillStyle='#1A1A2E';nctx.fillRect(0,0,120,120);if(nxt)for(let r=0;r<nxt.length;r++)for(let c=0;c<nxt[r].length;c++)if(nxt[r][c]){nctx.fillStyle=COLORS[(pts/100)%7];nctx.fillRect(c*24+12,r*24+12,22,22)}}
function loop(){update();draw();requestAnimationFrame(loop)}
document.addEventListener('keydown',e=>{if(!run||gameOver)return;if(e.key==='ArrowLeft')move(-1);else if(e.key==='ArrowRight')move(1);else if(e.key==='ArrowDown'){tick=8;move(0)}else if(e.key==='ArrowUp')rotate();else if(e.key===' ')drop();if(e.key==='r'||e.key==='R'){initGrid();pts=0;score.textContent='0';gameOver=0;newPiece()}});
overlay.addEventListener('click',()=>{overlay.style.display='none';initGrid();pts=0;score.textContent='0';gameOver=0;newPiece();run=1});
initGrid();newPiece();
loop();
</script></body></html>";

    public static string DinoRunner => @"<!DOCTYPE html><html><head><meta charset=""UTF-8""><style>
*{margin:0;padding:0;box-sizing:border-box}
body{background:#0D0D14;display:flex;justify-content:center;align-items:center;height:100vh;overflow:hidden;font-family:'Segoe UI',sans-serif}
#wrap{text-align:center}
#score{color:#0066FF;font-size:22px;font-weight:bold;margin-bottom:10px}
canvas{border:2px solid #0066FF;border-radius:4px;background:#1A1A2E}
#overlay{position:absolute;top:0;left:0;width:100%;height:100%;display:flex;justify-content:center;align-items:center;background:rgba(13,13,20,0.8);cursor:pointer}
#overlay span{color:#0066FF;font-size:28px;font-weight:bold}
#info{color:#8888AA;font-size:12px;margin-top:8px}
</style></head><body>
<div id=""wrap"">
<div id=""score"">0</div>
<div style=""position:relative;display:inline-block"">
<canvas id=""c"" width=""600"" height=""250""></canvas>
<div id=""overlay""><span>▶ PLAY</span></div>
</div>
<div id=""info"">Space/Up to jump &bull; Down to duck &bull; R to restart</div>
</div>
<script>
const c=document.getElementById('c'),ctx=c.getContext('2d'),score=document.getElementById('score'),overlay=document.getElementById('overlay');
const W=600,H=250,G=200;let px=80,py=G,pvy=0,ground=0,speed=6,pts=0,run=0,obstacles=[],timer=0;
function jump(){if(py===G){pvy=-10}}
function duck(){py=G+20}
function unduck(){py=G}
function update(){if(!run)return;
pvy+=0.5;py+=pvy;if(py>G){py=G;pvy=0}
timer++;if(timer>40+Math.random()*60){timer=0;obstacles.push({x:W,w:18+Math.random()*12,h:30+Math.random()*20,ty:Math.random()>.5?'cactus':'bird'})}
obstacles.forEach(o=>{o.x-=speed;if(o.ty==='bird'&&o.x<px+20){o.y=G-60}else{o.y=G-o.h}});
obstacles=obstacles.filter(o=>o.x>-50);
obstacles.forEach(o=>{if(px+20>o.x&&px<o.x+o.w&&py+20>o.y&&py<o.y+o.h){run=0}});
pts++;score.textContent=Math.floor(pts/5);
speed=6+Math.floor(pts/500);
ground+=speed;if(ground>20)ground-=20}
function draw(){ctx.fillStyle='#1A1A2E';ctx.fillRect(0,0,W,H);
ctx.fillStyle='#333355';ctx.fillRect(0,G+3,W,2);
for(let g=0;g<W;g+=40){ctx.fillStyle=g%80===0?'#0066FF66':'#33335566';ctx.fillRect(((g-ground)%W+W)%W,G+5,15,2)}
ctx.fillStyle='#0066FF';ctx.fillRect(px,py-20,20,25);
ctx.fillStyle='#FFFFFF';ctx.beginPath();ctx.arc(px+17,py-12,4,0,Math.PI*2);ctx.fill();
obstacles.forEach(o=>{ctx.fillStyle='#FF4D8C';if(o.ty==='bird'){ctx.beginPath();ctx.arc(o.x+o.w/2,o.y+10,10,0,Math.PI*2);ctx.fill()}else{ctx.fillRect(o.x,o.y,o.w,o.h)}});
if(!run){ctx.fillStyle='rgba(13,13,20,0.7)';ctx.fillRect(0,0,W,H);ctx.fillStyle='#FF5252';ctx.font='24px sans-serif';ctx.textAlign='center';ctx.fillText('GAME OVER',300,120);ctx.fillStyle='#8888AA';ctx.font='14px sans-serif';ctx.fillText('Click to restart',300,145)}}
function loop(){update();draw();requestAnimationFrame(loop)}
document.addEventListener('keydown',e=>{if((e.key===' '||e.key==='ArrowUp')&&py===G)jump();if(e.key==='ArrowDown')duck();if(e.key==='r'||e.key==='R'){pts=0;obstacles=[];speed=6;timer=0;py=G;run=1}});
document.addEventListener('keyup',e=>{if(e.key==='ArrowDown')unduck()});
overlay.addEventListener('click',()=>{overlay.style.display='none';pts=0;obstacles=[];speed=6;timer=0;py=G;run=1});
loop();
</script></body></html>";
}
