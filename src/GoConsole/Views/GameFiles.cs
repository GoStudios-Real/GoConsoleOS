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

    public static string FlappyBird => @"<!DOCTYPE html><html><head><meta charset=""UTF-8""><style>
*{margin:0;padding:0;box-sizing:border-box}
body{background:#0D0D14;display:flex;justify-content:center;align-items:center;height:100vh;overflow:hidden;font-family:'Segoe UI',sans-serif}
#wrap{text-align:center}
#score{color:#0066FF;font-size:28px;font-weight:bold;margin-bottom:10px}
canvas{border:2px solid #0066FF;border-radius:4px;background:#1A1A2E}
#overlay{position:absolute;top:0;left:0;width:100%;height:100%;display:flex;justify-content:center;align-items:center;background:rgba(13,13,20,0.8);cursor:pointer}
#overlay span{color:#0066FF;font-size:28px;font-weight:bold}
#info{color:#8888AA;font-size:12px;margin-top:8px}
</style></head><body>
<div id=""wrap"">
<div id=""score"">0</div>
<div style=""position:relative;display:inline-block"">
<canvas id=""c"" width=""400"" height=""600""></canvas>
<div id=""overlay""><span>▶ PLAY</span></div>
</div>
<div id=""info"">Space/Click to flap &bull; R to restart</div>
</div>
<script>
const c=document.getElementById('c'),ctx=c.getContext('2d'),score=document.getElementById('score'),overlay=document.getElementById('overlay');
const W=400,H=600,GAP=160,PIPEW=52,BIRDX=80;let birdY=300,birdV=0,pipes=[],pts=0,run=0,tick=0,best=0;
function flap(){birdV=-7}
function addPipe(){const top=Math.random()*(H-GAP-100)+50;pipes.push({x:W,top,h:GAP,scored:false})}
function update(){if(!run)return;tick++;birdV+=0.4;birdY+=birdV;
if(birdY<0||birdY>H-16){run=0;best=Math.max(best,pts);return}
if(tick%90===0)addPipe();
pipes.forEach(p=>{p.x-=3;if(!p.scored&&p.x+PIPEW<BIRDX){p.scored=true;pts++;score.textContent=pts}
if(BIRDX+16>p.x&&BIRDX<p.x+PIPEW&&(birdY<p.top||birdY+16>p.top+p.h)){run=0;best=Math.max(best,pts)}});
pipes=pipes.filter(p=>p.x>-PIPEW)}
function draw(){ctx.fillStyle='#1A1A2E';ctx.fillRect(0,0,W,H);
ctx.fillStyle='#333355';ctx.fillRect(0,H-20,W,20);
pipes.forEach(p=>{ctx.fillStyle='#0066FF';ctx.fillRect(p.x,0,PIPEW,p.top);ctx.fillRect(p.x,p.top+p.h,PIPEW,H-p.top-p.h);
ctx.fillStyle='#0044CC';ctx.fillRect(p.x-2,p.top-8,PIPEW+4,8);ctx.fillRect(p.x-2,p.top+p.h,PIPEW+4,8)});
ctx.fillStyle='#FFD600';ctx.beginPath();ctx.ellipse(BIRDX+8,birdY+8,10,8,0,0,Math.PI*2);ctx.fill();
ctx.fillStyle='#FFF';ctx.beginPath();ctx.arc(BIRDX+12,birdY+4,3,0,Math.PI*2);ctx.fill();
ctx.fillStyle='#FF6600';ctx.beginPath();ctx.moveTo(BIRDX+16,birdY+6);ctx.lineTo(BIRDX+22,birdY+8);ctx.lineTo(BIRDX+16,birdY+10);ctx.fill();
if(!run){ctx.fillStyle='rgba(13,13,20,0.7)';ctx.fillRect(0,0,W,H);ctx.fillStyle='#FF5252';ctx.font='28px sans-serif';ctx.textAlign='center';ctx.fillText('GAME OVER',200,270);ctx.fillStyle='#FFD600';ctx.font='16px sans-serif';ctx.fillText('Score: '+pts+'  Best: '+best,200,300);ctx.fillStyle='#8888AA';ctx.font='14px sans-serif';ctx.fillText('Click to restart',200,330)}}
function loop(){update();draw();requestAnimationFrame(loop)}
document.addEventListener('keydown',e=>{if(e.key===' '||e.key==='ArrowUp')flap();if(e.key==='r'||e.key==='R'){birdY=300;birdV=0;pipes=[];pts=0;score.textContent='0';tick=0;run=1}});
c.addEventListener('click',()=>{if(!run){birdY=300;birdV=0;pipes=[];pts=0;score.textContent='0';tick=0;run=1}else flap()});
overlay.addEventListener('click',()=>{overlay.style.display='none';birdY=300;birdV=0;pipes=[];pts=0;score.textContent='0';tick=0;run=1});
loop();
</script></body></html>";

    public static string SpaceInvaders => @"<!DOCTYPE html><html><head><meta charset=""UTF-8""><style>
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
<canvas id=""c"" width=""480"" height=""400""></canvas>
<div id=""overlay""><span>▶ PLAY</span></div>
</div>
<div id=""info"">← → to move &bull; Space to shoot &bull; R to restart</div>
</div>
<script>
const c=document.getElementById('c'),ctx=c.getContext('2d'),score=document.getElementById('score'),overlay=document.getElementById('overlay');
const W=480,H=400;let px=W/2,aliens=[],bullets=[],eBullets=[],dir=1,pts=0,run=0,tick=0,lives=3;
function initAliens(){aliens=[];for(let r=0;r<4;r++)for(let col=0;col<8;col++)aliens.push({x:40+col*50,y:40+r*35,w:30,h:20,alive:true,ty:r<1?'ufo':r<3?'mid':'low'})}
function shoot(){if(bullets.length<3)bullets.push({x:px,y:H-40})}
function update(){if(!run)return;tick++;
if(tick%60===0){aliens.forEach(a=>{if(a.alive&&Math.random()<0.3)eBullets.push({x:a.x+15,y:a.y+20})})}
let edge=false;aliens.forEach(a=>{if(a.alive){a.x+=dir*0.5;if(a.x<=0||a.x>=W-30)edge=true}});
if(edge){dir*=-1;aliens.forEach(a=>{if(a.alive)a.y+=8})}
bullets.forEach(b=>{b.y-=8});
bullets=bullets.filter(b=>b.y>-10);
eBullets.forEach(b=>{b.y+=4});
eBullets=eBullets.filter(b=>b.y<H+10);
bullets.forEach(b=>{aliens.forEach(a=>{if(!a.alive)return;if(b.x>a.x&&b.x<a.x+a.w&&b.y>a.y&&b.y<a.y+a.h){a.alive=false;b.y=-100;pts+=a.ty==='ufo'?30:a.ty==='mid'?20:10;score.textContent=pts}})});
eBullets.forEach(b=>{if(b.x>px-8&&b.x<px+8&&b.y>H-50){b.y=H+100;lives--}});
if(lives<=0){run=0}
if(aliens.every(a=>!a.alive)){initAliens()}
aliens.forEach(a=>{if(a.alive&&a.y+20>H-40)run=0})}
function draw(){ctx.fillStyle='#1A1A2E';ctx.fillRect(0,0,W,H);
aliens.forEach(a=>{if(!a.alive)return;
if(a.ty==='ufo'){ctx.fillStyle='#FF4D8C';ctx.fillRect(a.x,a.y,a.w,a.h);ctx.fillStyle='#FF8C00';ctx.fillRect(a.x+8,a.y+4,14,12)}
else if(a.ty==='mid'){ctx.fillStyle='#7B2DFF';ctx.fillRect(a.x,a.y,a.w,a.h);ctx.fillStyle='#AAA';ctx.fillRect(a.x+6,a.y+3,18,14)}
else{ctx.fillStyle='#0066FF';ctx.fillRect(a.x,a.y,a.w,a.h);ctx.fillStyle='#FFF';ctx.fillRect(a.x+8,a.y+4,14,12)}});
ctx.fillStyle='#00E676';ctx.fillRect(px-8,H-40,16,20);ctx.fillRect(px-14,H-28,28,6);
ctx.fillStyle='#0066FF';ctx.fillRect(px-2,H-48,4,10);
bullets.forEach(b=>{ctx.fillStyle='#FFD600';ctx.fillRect(b.x-1,b.y,2,10)});
eBullets.forEach(b=>{ctx.fillStyle='#FF5252';ctx.fillRect(b.x-1,b.y,2,10)});
for(let i=0;i<lives;i++){ctx.fillStyle='#00E676';ctx.fillRect(10+i*20,10,12,12)}
if(!run){ctx.fillStyle='rgba(13,13,20,0.7)';ctx.fillRect(0,0,W,H);ctx.fillStyle='#FF5252';ctx.font='24px sans-serif';ctx.textAlign='center';ctx.fillText('GAME OVER',240,190);ctx.fillStyle='#8888AA';ctx.font='14px sans-serif';ctx.fillText('Click to restart',240,220)}}
function loop(){update();draw();requestAnimationFrame(loop)}
document.addEventListener('keydown',e=>{if(e.key==='ArrowLeft')px=Math.max(20,px-8);if(e.key==='ArrowRight')px=Math.min(W-20,px+8);if(e.key===' ')shoot();if(e.key==='r'||e.key==='R'){pts=0;score.textContent='0';lives=3;initAliens();px=W/2;bullets=[];eBullets=[];run=1}});
overlay.addEventListener('click',()=>{overlay.style.display='none';pts=0;score.textContent='0';lives=3;initAliens();px=W/2;bullets=[];eBullets=[];run=1});
initAliens();loop();
</script></body></html>";

    public static string Game2048 => @"<!DOCTYPE html><html><head><meta charset=""UTF-8""><style>
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
<div id=""info"">Arrow keys to slide tiles &bull; R to restart</div>
</div>
<script>
const c=document.getElementById('c'),ctx=c.getContext('2d'),score=document.getElementById('score'),overlay=document.getElementById('overlay');
const S=90,P=10;let grid,pts,run=0;
const COLORS={0:'#1A1A2E',2:'#333366',4:'#3D4DAA',8:'#FF8C00',16:'#FF6600',32:'#FF4D8C',64:'#FF0066',128:'#FFD600',256:'#FFC800',512:'#FFB300',1024:'#00E676',2048:'#0066FF'};
function init(){grid=Array.from({length:4},()=>Array(4).fill(0));pts=0;score.textContent='0';spawn();spawn()}
function spawn(){const empty=[];for(let r=0;r<4;r++)for(let c=0;c<4;c++)if(!grid[r][c])empty.push([r,c]);if(!empty.length)return;const[r,c]=empty[Math.floor(Math.random()*empty.length)];grid[r][c]=Math.random()<.9?2:4}
function slide(row){let a=row.filter(v=>v);for(let i=0;i<a.length-1;i++){if(a[i]===a[i+1]){a[i]*=2;pts+=a[i];a.splice(i+1,1)}}
while(a.length<4)a.push(0);return a}
function move(dir){let moved=false;const g=grid.map(r=>[...r]);
if(dir==='l'){for(let r=0;r<4;r++){const n=slide(g[r]);if(n.some((v,i)=>v!==g[r][i])){grid[r]=n;moved=true}}}
if(dir==='r'){for(let r=0;r<4;r++){const n=slide(g[r].reverse()).reverse();if(n.some((v,i)=>v!==g[r][i])){grid[r]=n;moved=true}}}
if(dir==='u'){for(let c=0;c<4;c++){const col=[g[0][c],g[1][c],g[2][c],g[3][c]];const n=slide(col);if(n.some((v,i)=>v!==col[i])){for(let r=0;r<4;r++)grid[r][c]=n[r];moved=true}}}
if(dir==='d'){for(let c=0;c<4;c++){const col=[g[3][c],g[2][c],g[1][c],g[0][c]];const n=slide(col);if(n.some((v,i)=>v!==col[i])){for(let r=0;r<4;r++)grid[r][c]=n[3-r];moved=true}}}
if(moved){spawn();score.textContent=pts}}
function draw(){ctx.fillStyle='#1A1A2E';ctx.fillRect(0,0,400,400);
for(let r=0;r<4;r++)for(let col=0;col<4;col++){const x=col*(S+P)+P,y=r*(S+P)+P,v=grid[r][col];
ctx.fillStyle=COLORS[v]||'#1A1A2E';ctx.fillRect(x,y,S,S);
if(v){ctx.fillStyle=v>=128?'#FFF':'#CCC';ctx.font='bold '+((v>=1024?28:36))+'px sans-serif';ctx.textAlign='center';ctx.fillText(v,x+S/2,y+S/2+12)}}
if(!run){ctx.fillStyle='rgba(13,13,20,0.7)';ctx.fillRect(0,0,400,400);ctx.fillStyle='#FFD600';ctx.font='24px sans-serif';ctx.textAlign='center';ctx.fillText('Score: '+pts,200,190);ctx.fillStyle='#8888AA';ctx.font='14px sans-serif';ctx.fillText('Click to restart',200,220)}}
document.addEventListener('keydown',e=>{if(!run)return;if(e.key==='ArrowLeft')move('l');if(e.key==='ArrowRight')move('r');if(e.key==='ArrowUp')move('u');if(e.key==='ArrowDown')move('d');if(e.key==='r'||e.key==='R'){init();run=1}});
overlay.addEventListener('click',()=>{overlay.style.display='none';init();run=1});
init();draw();
</script></body></html>";

    public static string MemoryMatch => @"<!DOCTYPE html><html><head><meta charset=""UTF-8""><style>
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
<div id=""score"">Moves: 0</div>
<div style=""position:relative;display:inline-block"">
<canvas id=""c"" width=""400"" height=""400""></canvas>
<div id=""overlay""><span>▶ PLAY</span></div>
</div>
<div id=""info"">Click cards to match pairs &bull; R to restart</div>
</div>
<script>
const c=document.getElementById('c'),ctx=c.getContext('2d'),score=document.getElementById('score'),overlay=document.getElementById('overlay');
const ROWS=4,COLS=4,S=90,P=5;const EMOJIS=['🎮','🏆','⭐','🔥','💎','🎵','🚀','🎯'];
let cards,flipped,matched,moves,run=0,locked=false;
function init(){const pairs=[...EMOJIS,...EMOJIS];for(let i=pairs.length-1;i>0;i--){const j=Math.floor(Math.random()*(i+1));[pairs[i],pairs[j]]=[pairs[j],pairs[i]]}
cards=pairs.map((e,i)=>({emoji:e,row:Math.floor(i/COLS),col:i%COLS,show:false,done:false}));
flipped=[];matched=0;moves=0;score.textContent='Moves: 0'}
function draw(){ctx.fillStyle='#1A1A2E';ctx.fillRect(0,0,400,400);
cards.forEach(card=>{const x=card.col*(S+P)+P,y=card.row*(S+P)+P;
if(card.done||card.show){ctx.fillStyle=card.done?'#0066FF33':'#333366';ctx.fillRect(x,y,S,S);
ctx.font='36px sans-serif';ctx.textAlign='center';ctx.fillText(card.emoji,x+S/2,y+S/2+12);
if(card.done){ctx.strokeStyle='#0066FF';ctx.lineWidth=2;ctx.strokeRect(x+2,y+2,S-4,S-4)}}
else{ctx.fillStyle='#2A2A44';ctx.fillRect(x,y,S,S);ctx.fillStyle='#0066FF';ctx.font='24px sans-serif';ctx.textAlign='center';ctx.fillText('?',x+S/2,y+S/2+8)}});
if(matched===EMOJIS.length){ctx.fillStyle='rgba(13,13,20,0.7)';ctx.fillRect(0,0,400,400);ctx.fillStyle='#00E676';ctx.font='28px sans-serif';ctx.textAlign='center';ctx.fillText('YOU WIN!',200,180);ctx.fillStyle='#FFD600';ctx.font='18px sans-serif';ctx.fillText(moves+' moves',200,210);ctx.fillStyle='#8888AA';ctx.font='14px sans-serif';ctx.fillText('Click to play again',200,240);run=0}}
c.addEventListener('click',e=>{if(!run||locked)return;const r=c.getBoundingClientRect();const mx=e.clientX-r.left,my=e.clientY-r.top;
const col=Math.floor(mx/(S+P)),row=Math.floor(my/(S+P));
const card=cards.find(c=>c.row===row&&c.col===col&&!c.done&&!c.show);
if(!card)return;card.show=true;flipped.push(card);
if(flipped.length===2){locked=true;moves++;score.textContent='Moves: '+moves;
if(flipped[0].emoji===flipped[1].emoji){flipped[0].done=true;flipped[1].done=true;matched++;flipped=[];locked=false}
else{setTimeout(()=>{flipped[0].show=false;flipped[1].show=false;flipped=[];locked=false},600)}}});
document.addEventListener('keydown',e=>{if(e.key==='r'||e.key==='R'){init();run=1}});
overlay.addEventListener('click',()=>{overlay.style.display='none';init();run=1});
init();
</script></body></html>";

    public static string Minesweeper => @"<!DOCTYPE html><html><head><meta charset=""UTF-8""><style>
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
<div id=""score"">💣 10 &bull; ⏱ 0s</div>
<div style=""position:relative;display:inline-block"">
<canvas id=""c"" width=""400"" height=""400""></canvas>
<div id=""overlay""><span>▶ PLAY</span></div>
</div>
<div id=""info"">Left click to reveal &bull; Right click to flag &bull; R to restart</div>
</div>
<script>
const c=document.getElementById('c'),ctx=c.getContext('2d'),score=document.getElementById('score'),overlay=document.getElementById('overlay');
const ROWS=12,COLS=12,S=32,P=2;let grid,revealed,flagged,mines=15,run=0,gameOver=0,timer=0;
function init(){grid=Array.from({length:ROWS},()=>Array(COLS).fill(0));revealed=Array.from({length:ROWS},()=>Array(COLS).fill(false));flagged=Array.from({length:ROWS},()=>Array(COLS).fill(false));
let placed=0;while(placed<mines){const r=Math.floor(Math.random()*ROWS),col=Math.floor(Math.random()*COLS);if(grid[r][col]!==-1){grid[r][col]=-1;placed++}}
for(let r=0;r<ROWS;r++)for(let col=0;col<COLS;col++){if(grid[r][col]===-1)continue;let count=0;for(let dr=-1;dr<=1;dr++)for(let dc=-1;dc<=1;dc++){const nr=r+dr,nc=col+dc;if(nr>=0&&nr<ROWS&&nc>=0&&nc<COLS&&grid[nr][nc]===-1)count++}grid[r][col]=count}
run=1;gameOver=0;timer=0;score.textContent='💣 '+mines+' ⏱ 0s'}
function reveal(r,col){if(r<0||r>=ROWS||col<0||col>=COLS||revealed[r][col]||flagged[r][col])return;revealed[r][col]=true;
if(grid[r][col]===0){for(let dr=-1;dr<=1;dr++)for(let dc=-1;dc<=1;dc++)reveal(r+dr,col+dc)}}
function checkWin(){let count=0;for(let r=0;r<ROWS;r++)for(let col=0;col<COLS;col++)if(revealed[r][col])count++;return count===ROWS*COLS-mines}
const NUMCOLORS=['','#0066FF','#00E676','#FF4D8C','#7B2DFF','#FF8C00','#FFD600','#FF0066','#FFFFFF'];
function draw(){ctx.fillStyle='#1A1A2E';ctx.fillRect(0,0,400,400);
for(let r=0;r<ROWS;r++)for(let col=0;col<COLS;col++){const x=col*(S+P)+P,y=r*(S+P)+P;
if(revealed[r][col]){ctx.fillStyle=grid[r][col]===-1?'#FF5252':'#222244';ctx.fillRect(x,y,S,S);
if(grid[r][col]===-1){ctx.font='16px sans-serif';ctx.textAlign='center';ctx.fillText('💣',x+S/2,y+S/2+5)}
else if(grid[r][col]>0){ctx.fillStyle=NUMCOLORS[grid[r][col]];ctx.font='bold 14px sans-serif';ctx.textAlign='center';ctx.fillText(grid[r][col],x+S/2,y+S/2+5)}}
else{ctx.fillStyle=flagged[r][col]?'#FFD60033':'#2A2A44';ctx.fillRect(x,y,S,S);
if(flagged[r][col]){ctx.font='14px sans-serif';ctx.textAlign='center';ctx.fillText('🚩',x+S/2,y+S/2+5)}}}
if(gameOver){ctx.fillStyle='rgba(13,13,20,0.7)';ctx.fillRect(0,0,400,400);ctx.fillStyle=run?'#00E676':'#FF5252';ctx.font='24px sans-serif';ctx.textAlign='center';ctx.fillText(run?'YOU WIN!':'GAME OVER',200,190);ctx.fillStyle='#8888AA';ctx.font='14px sans-serif';ctx.fillText('Click to restart',200,220)}}
c.addEventListener('click',e=>{if(!run||gameOver)return;const r=c.getBoundingClientRect();const mx=e.clientX-r.left,my=e.clientY-r.top;
const col=Math.floor(mx/(S+P)),row=Math.floor(my/(S+P));if(row<0||row>=ROWS||col<0||col>=COLS)return;
if(grid[row][col]===-1){gameOver=0;run=0;for(let r2=0;r2<ROWS;r2++)for(let c2=0;c2<COLS;c2++)if(grid[r2][c2]===-1)revealed[r2][c2]=true}
else{reveal(row,col);if(checkWin())gameOver=run=0}});
c.addEventListener('contextmenu',e=>{e.preventDefault();if(!run||gameOver)return;const r=c.getBoundingClientRect();const mx=e.clientX-r.left,my=e.clientY-r.top;
const col=Math.floor(mx/(S+P)),row=Math.floor(my/(S+P));if(row>=0&&row<ROWS&&col>=0&&col<COLS&&!revealed[row][col])flagged[row][col]=!flagged[row][col]});
setInterval(()=>{if(run&&!gameOver){timer++;score.textContent='💣 '+(mines)+' ⏱ '+timer+'s'}},1000);
document.addEventListener('keydown',e=>{if(e.key==='r'||e.key==='R')init()});
overlay.addEventListener('click',()=>{overlay.style.display='none';init()});
init();
</script></body></html>";
}
