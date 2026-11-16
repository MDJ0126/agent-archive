-- Executed by the real Aseprite Lua API. All drawing and exports happen in Aseprite.
local root = app.params.root or 'C:/Projects/DotAction'
if app.fs.isFile(root..'/art/previews/reference-board.aseprite') and app.params.overwrite~='true' then
 error('Existing native art is protected. Edit it in Aseprite; regenerate only with explicit overwrite=true.')
end
local pc = app.pixelColor
local C = {}
local ramps = {
 N={'151522','29283C','45435B','716B80','AAA0AA','E7DCCB'},
 G={'49312D','80503A','BF8147','F2C66D'}, T={'183E46','24646B','3D9C9B','83D6BE'},
 R={'542C3C','944154','D76564','F5A081'}, V={'352D55','614879','9772AE','D2ACE0'},
 L={'293F35','46694C','7DA35B','C1CE83'}, B={'273854','46618B','7C9FBE','BDD6DF'}, S={'A87359','E2B58A'}
}
for group, hexes in pairs(ramps) do
 for i,h in ipairs(hexes) do C[group..(i-1)] = pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255) end
end
local function rect(im,x,y,w,h,c)
 for yy=y,y+h-1 do for xx=x,x+w-1 do if xx>=0 and yy>=0 and xx<im.width and yy<im.height then im:drawPixel(xx,yy,C[c] or c) end end end
end
local function line(im,x0,y0,x1,y1,c)
 local dx,dy=math.abs(x1-x0),-math.abs(y1-y0)
 local sx,sy=x0<x1 and 1 or -1,y0<y1 and 1 or -1
 local err=dx+dy
 while true do
  rect(im,x0,y0,1,1,c)
  if x0==x1 and y0==y1 then break end
  local e=2*err
  if e>=dy then err=err+dy;x0=x0+sx end
  if e<=dx then err=err+dx;y0=y0+sy end
 end
end
local function poly(im,pts,c)
 local minY,maxY=im.height,0
 for _,p in ipairs(pts) do minY=math.min(minY,p[2]);maxY=math.max(maxY,p[2]) end
 for y=minY,maxY do
  local crossings={}
  for i,p in ipairs(pts) do
   local q=pts[i%#pts+1]
   if (p[2]<=y and q[2]>y) or (q[2]<=y and p[2]>y) then crossings[#crossings+1]=p[1]+(y-p[2])*(q[1]-p[1])/(q[2]-p[2]) end
  end
  table.sort(crossings)
  for i=1,#crossings-1,2 do for x=math.ceil(crossings[i]),math.floor(crossings[i+1]) do rect(im,x,y,1,1,c) end end
 end
 for i,p in ipairs(pts) do local q=pts[i%#pts+1];line(im,p[1],p[2],q[1],q[2],c) end
end
local function frame(im,x,y,w,h,c)
 rect(im,x+1,y,w-2,1,c);rect(im,x+1,y+h-1,w-2,1,c)
 rect(im,x,y+1,1,h-2,c);rect(im,x+w-1,y+1,1,h-2,c)
end
local function sprite(w,h)
 local s=Sprite(w,h,ColorMode.RGB)
 s:setPalette(Palette{fromFile=root..'/art/palettes/dotaction-32.gpl'})
 s.layers[1].name='body'
 return s
end
local function layer(s,name)
 local l=s:newLayer();l.name=name
 local im=Image(s.width,s.height,ColorMode.RGB)
 s:newCel(l,s.frames[1],im,Point(0,0))
 return l.cels[1].image,l
end
local function flatten(s)
 local im=Image(s.width,s.height,ColorMode.RGB)
 im:drawSprite(s,1)
 return im
end
local function save(s,rel,id)
 s:saveAs(root..'/art/source/'..rel..'/'..id..'.aseprite')
 flatten(s):saveAs(root..'/art/exports/'..rel..'/'..id..'.png')
end

-- Player: rounded head, teal cloth, brass details, right-facing silhouette.
local player=sprite(32,32)
local p=player.layers[1].cels[1].image
poly(p,{{11,15},{19,14},{22,19},{21,23},{19,25},{20,28},{15,28},{14,24},{13,28},{8,28},{10,23},{9,19}},'N0')
poly(p,{{12,16},{18,15},{20,19},{18,23},{11,23},{11,19}},'T0')
rect(p,12,16,5,6,'T1');rect(p,12,17,2,4,'T2')
rect(p,10,24,3,3,'N2');rect(p,16,24,3,3,'N1')
rect(p,9,27,4,1,'G1');rect(p,16,27,4,1,'G1')
poly(p,{{12,5},{18,4},{21,6},{21,10},{23,11},{22,13},{20,13},{19,16},{14,16},{11,12},{10,8}},'N0')
poly(p,{{13,6},{18,5},{20,7},{20,10},{21,11},{20,12},{18,12},{18,14},{14,14},{12,11}},'S0')
rect(p,14,7,5,5,'S1');rect(p,18,9,2,1,'N0');rect(p,19,8,1,1,'N5')
poly(p,{{10,5},{13,3},{18,3},{20,5},{19,7},{12,8},{11,11},{9,9}},'N0')
poly(p,{{11,5},{14,4},{18,4},{19,5},{17,6},{11,7}},'N2')
rect(p,12,5,4,1,'N3');rect(p,11,9,2,3,'G0')
local pe=layer(player,'equipment')
poly(pe,{{11,13},{18,13},{20,15},{18,17},{12,16},{8,19},{6,17}},'N0')
poly(pe,{{11,14},{17,14},{18,15},{12,15},{8,17},{7,17}},'T2');rect(pe,12,14,5,1,'T3')
rect(pe,10,22,10,2,'G0');rect(pe,11,22,8,1,'G2');rect(pe,15,22,2,2,'G3')
poly(pe,{{19,17},{22,17},{23,20},{21,22},{19,21}},'N0');rect(pe,20,18,2,3,'S0');rect(pe,20,18,1,2,'S1')
poly(pe,{{22,18},{25,16},{29,9},{30,9},{29,15},{25,21},{23,21}},'N0')
poly(pe,{{24,18},{28,12},{29,11},{28,15},{25,19}},'B2');line(pe,25,17,29,11,'B3');line(pe,22,19,25,21,'G3')
layer(player,'fx')
local pg,pl=layer(player,'guides');line(pg,13,28,19,28,'R2');pl.isVisible=false
local ps=player:newLayer();ps.name='shadow';ps.stackIndex=1
local psi=Image(32,32,ColorMode.RGB);rect(psi,7,29,17,1,'N1');player:newCel(ps,1,psi)
save(player,'characters/player','chr_player')

-- Enemy: angular helmet, dark steel, narrow red visor, left-facing.
local enemy=sprite(32,32)
local e=enemy.layers[1].cels[1].image
poly(e,{{8,13},{21,12},{25,17},{25,23},{22,25},{24,28},{18,28},{16,24},{14,28},{7,28},{9,24},{6,20}},'N0')
poly(e,{{10,14},{20,14},{23,18},{21,23},{11,23},{8,19}},'B0')
poly(e,{{10,15},{16,14},{16,22},{11,22},{9,19}},'B1');rect(e,11,16,2,4,'B2')
rect(e,9,24,4,3,'N2');rect(e,19,24,3,3,'N1');rect(e,8,27,6,1,'G1');rect(e,18,27,6,1,'G1')
poly(e,{{8,6},{12,3},{14,6},{21,5},{24,8},{23,14},{19,17},{9,15},{7,11}},'N0')
poly(e,{{9,7},{12,5},{14,7},{20,6},{22,8},{21,13},{18,15},{10,13},{9,10}},'B1')
poly(e,{{10,7},{12,6},{14,8},{20,7},{21,8},{17,9},{10,9}},'B2')
rect(e,8,10,10,3,'N0');rect(e,9,11,6,1,'R2');rect(e,9,11,2,1,'R3');rect(e,18,10,3,3,'B0')
local ee=layer(enemy,'equipment')
poly(ee,{{6,15},{10,13},{13,15},{11,19},{6,19},{4,17}},'N0');poly(ee,{{7,15},{10,14},{11,15},{10,17},{6,17}},'G2');rect(ee,7,15,3,1,'G3')
rect(ee,11,22,10,2,'G0');rect(ee,12,22,8,1,'G2');rect(ee,16,21,3,3,'G1');rect(ee,17,22,1,1,'R2')
poly(ee,{{5,18},{8,19},{8,23},{5,24},{3,22},{3,20}},'N0');rect(ee,4,20,3,3,'B1');rect(ee,4,20,1,2,'B2')
poly(ee,{{3,14},{6,14},{6,18},{4,20},{2,19},{1,16}},'N0');rect(ee,3,15,2,3,'G2');rect(ee,3,15,1,1,'G3')
layer(enemy,'fx');local eg,el=layer(enemy,'guides');line(eg,13,28,19,28,'R2');el.isVisible=false
local es=enemy:newLayer();es.name='shadow';es.stackIndex=1
local esi=Image(32,32,ColorMode.RGB);rect(esi,5,29,21,1,'N1');enemy:newCel(es,1,esi)
save(enemy,'characters/enemies','chr_enemy_scrapper')

local symbols={}
local blade=sprite(16,16);local b=blade.layers[1].cels[1].image
poly(b,{{4,10},{10,3},{13,2},{12,6},{7,11}},'N0')
poly(b,{{5,9},{11,3},{12,3},{11,6},{7,10}},'B2');line(b,6,8,11,3,'B3')
line(b,3,9,7,13,'N0');line(b,3,10,6,13,'G2');rect(b,4,10,1,1,'G3');line(b,3,12,5,10,'G1');rect(b,2,13,2,1,'N0')
save(blade,'symbols','sym_blade');symbols[1]=flatten(blade)
local bolt=sprite(16,16);local z=bolt.layers[1].cels[1].image
poly(z,{{8,2},{12,2},{9,6},{13,6},{6,13},{4,13},{6,9},{3,9}},'N0')
poly(z,{{9,3},{10,3},{7,7},{11,7},{6,12},{8,8},{5,8}},'T2');line(z,9,3,6,7,'T3');line(z,8,8,6,10,'T3')
save(bolt,'symbols','sym_bolt');symbols[2]=flatten(bolt)
local guard=sprite(16,16);local g=guard.layers[1].cels[1].image
poly(g,{{3,3},{7,2},{12,3},{12,8},{10,11},{7,13},{4,11},{3,8}},'N0')
poly(g,{{4,4},{7,3},{11,4},{11,8},{9,10},{7,12},{5,10},{4,8}},'G2')
poly(g,{{5,5},{7,4},{10,5},{10,8},{7,11},{5,8}},'B1');line(g,6,5,6,8,'B2');line(g,7,4,7,10,'G3')
save(guard,'symbols','sym_guard');symbols[3]=flatten(guard)

local cell=sprite(32,32);cell.layers[1].name='frame';local ci=cell.layers[1].cels[1].image
rect(ci,1,2,30,28,'N0');rect(ci,2,1,28,30,'N0');frame(ci,2,2,28,28,'G1');frame(ci,3,3,26,26,'G2')
line(ci,4,3,27,3,'G3');line(ci,3,4,3,27,'G3');line(ci,5,28,27,28,'G0');line(ci,28,5,28,27,'G0')
rect(ci,5,5,22,22,'N1');frame(ci,5,5,22,22,'N0');line(ci,7,7,24,7,'N2')
for _,q in ipairs({{4,4},{27,4},{4,27},{27,27}}) do rect(ci,q[1],q[2],1,1,'N5') end
save(cell,'ui','ui_slot_cell')

local card=sprite(96,128);card.layers[1].name='frame';local ca=card.layers[1].cels[1].image
rect(ca,3,1,90,126,'N0');rect(ca,1,3,94,122,'N0');rect(ca,4,3,88,122,'G0');frame(ca,3,3,90,122,'G2');frame(ca,5,5,86,118,'G1')
rect(ca,7,7,82,114,'N1');line(ca,5,5,90,5,'G3');line(ca,5,5,5,120,'G3')
rect(ca,8,8,80,16,'V0');line(ca,8,24,87,24,'G1')
frame(ca,32,28,32,32,'G0');frame(ca,33,29,30,30,'V1');rect(ca,34,30,28,28,'N0')
line(ca,12,64,83,64,'G0');rect(ca,46,63,3,3,'V2');rect(ca,47,63,1,1,'V3')
rect(ca,8,68,80,36,'N1');line(ca,8,107,87,107,'G1');rect(ca,8,110,80,10,'V0')
for _,x in ipairs({8,85}) do for _,y in ipairs({27,104}) do rect(ca,x,y,3,3,'G1');rect(ca,x,y,1,1,'G3') end end
save(card,'cards','ui_card_frame')
local tile=sprite(16,16);tile.layers[1].name='stone';local t=tile.layers[1].cels[1].image
rect(t,0,0,16,16,'N1');line(t,0,0,15,0,'N2');line(t,0,8,15,8,'N0');line(t,7,1,7,7,'N0');line(t,2,9,2,15,'N0');rect(t,1,2,3,1,'N2');rect(t,10,10,3,1,'N2')
save(tile,'environment','tile_floor')

-- Small bitmap lettering is drawn through Aseprite, only on preview layers.
local font={
 A={'01110','10001','10001','11111','10001','10001','10001'},B={'11110','10001','10001','11110','10001','10001','11110'},
 C={'01111','10000','10000','10000','10000','10000','01111'},D={'11110','10001','10001','10001','10001','10001','11110'},
 E={'11111','10000','10000','11110','10000','10000','11111'},F={'11111','10000','10000','11110','10000','10000','10000'},
 G={'01111','10000','10000','10111','10001','10001','01111'},H={'10001','10001','10001','11111','10001','10001','10001'},
 I={'111','010','010','010','010','010','111'},J={'00111','00010','00010','00010','10010','10010','01100'},
 K={'10001','10010','10100','11000','10100','10010','10001'},L={'10000','10000','10000','10000','10000','10000','11111'},
 M={'10001','11011','10101','10101','10001','10001','10001'},N={'10001','11001','10101','10011','10001','10001','10001'},
 O={'01110','10001','10001','10001','10001','10001','01110'},P={'11110','10001','10001','11110','10000','10000','10000'},
 Q={'01110','10001','10001','10001','10101','10010','01101'},R={'11110','10001','10001','11110','10100','10010','10001'},
 S={'01111','10000','10000','01110','00001','00001','11110'},T={'11111','00100','00100','00100','00100','00100','00100'},
 U={'10001','10001','10001','10001','10001','10001','01110'},V={'10001','10001','10001','10001','10001','01010','00100'},
 W={'10001','10001','10001','10101','10101','10101','01010'},X={'10001','10001','01010','00100','01010','10001','10001'},
 Y={'10001','10001','01010','00100','00100','00100','00100'},Z={'11111','00001','00010','00100','01000','10000','11111'},
 ['0']={'01110','10001','10011','10101','11001','10001','01110'},['1']={'010','110','010','010','010','010','111'},
 ['2']={'01110','10001','00001','00010','00100','01000','11111'},['3']={'11110','00001','00001','01110','00001','00001','11110'},
 ['4']={'10010','10010','10010','11111','00010','00010','00010'},['6']={'01110','10000','10000','11110','10001','10001','01110'},
 ['8']={'01110','10001','10001','01110','10001','10001','01110'},['9']={'01110','10001','10001','01111','00001','00001','01110'},
 ['-']={'00000','00000','00000','11111','00000','00000','00000'},['.']={'0','0','0','0','0','0','1'},['/']={'00001','00001','00010','00100','01000','10000','10000'}
}
local function text(im,str,x,y,c,scale)
 scale=scale or 1
 for char in str:upper():gmatch('.') do
  local glyph=font[char]
  if glyph then for yy,row in ipairs(glyph) do for xx=1,#row do if row:sub(xx,xx)=='1' then rect(im,x+(xx-1)*scale,y+(yy-1)*scale,scale,scale,c) end end end;x=x+(#glyph[1]+1)*scale else x=x+4*scale end
 end
end
local board=sprite(640,360);board.layers[1].name='background';local bg=board.layers[1].cels[1].image
rect(bg,0,0,640,360,'N0');rect(bg,16,16,608,328,'N1');rect(bg,16,16,608,3,'G2')
text(bg,'DOT ACTION',32,30,'N5',2);text(bg,'ART BASELINE / 01',400,35,'G3')
text(bg,'SHARED PIXELS. SHARED SYMBOLS.',32,53,'N4')
frame(bg,31,76,392,216,'N2');frame(bg,439,76,170,216,'N2')
text(bg,'COMBAT / NATIVE SCALE',44,88,'N4');text(bg,'AUGMENT',454,88,'V3')
-- Low-contrast environment is part of the comparison, not a game screenshot.
rect(bg,40,104,374,175,'N0');rect(bg,48,112,358,139,'N1')
for x=48,399,16 do bg:drawImage(flatten(tile),Point(x,248));bg:drawImage(flatten(tile),Point(x,264)) end
line(bg,48,247,405,247,'N3')
rect(bg,64,126,12,120,'N0');rect(bg,67,128,2,114,'N2');rect(bg,380,126,12,120,'N0');rect(bg,383,128,2,114,'N2')
local art=layer(board,'assets_native_1x')
art:drawImage(flatten(player),Point(124,219));art:drawImage(flatten(enemy),Point(272,219))
for i=1,3 do art:drawImage(flatten(cell),Point(192+(i-1)*32,132));art:drawImage(symbols[i],Point(200+(i-1)*32,140)) end
art:drawImage(flatten(card),Point(476,110));art:drawImage(symbols[2],Point(516,146))
text(art,'BOLT AUGMENT',484,122,'N5');text(art,'BOLT EFFECT',484,182,'N5');text(art,'UPGRADE',484,194,'T3');text(art,'AUGMENT',484,222,'V3')
rect(art,556,222,2,3,'V3');rect(art,560,222,2,3,'V3')
text(art,'PLAYER',122,263,'T3');text(art,'SCRAPPER',264,263,'R3');text(art,'SLOT',228,174,'G3')
text(bg,'32 SHARED COLORS',32,305,'N4')
local order={'N','G','T','R','V','L','B','S'};local n=0
for _,gr in ipairs(order) do for i=1,#ramps[gr] do rect(bg,160+n*13,302,11,11,gr..(i-1));n=n+1 end end
text(bg,'1X ASSETS / 1PX OUTLINE / TOP LEFT LIGHT',32,327,'N3')
bg:saveAs(root..'/art/previews/reference-background.png')
board:saveAs(root..'/art/previews/reference-board.aseprite')
flatten(board):saveAs(root..'/art/previews/reference-board.png')
app.activeSprite=board
app.command.SpriteSize{ui=false,width=2560,height=1440,method='nearest'}
flatten(board):saveAs(root..'/art/previews/reference-board-4x.png')
board:close()
-- Reopen every saved native source and export again to prove real Aseprite readability.
local checks=io.open(root..'/art/previews/aseprite-save-reopen.txt','w')
for _,s in ipairs({player,enemy,blade,bolt,guard,cell,card,tile}) do
 local filename=s.filename;s:close();local reopened=app.open(filename)
 checks:write(filename..' | '..reopened.width..'x'..reopened.height..' | layers='..#reopened.layers..' | modified='..tostring(reopened.isModified)..'\n')
 reopened:close()
end
local reopened=app.open(root..'/art/previews/reference-board.aseprite')
checks:write('reference-board | '..reopened.width..'x'..reopened.height..' | modified='..tostring(reopened.isModified)..'\n');reopened:close();checks:close()
print('Created 8 static native sources, PNG exports, native comparison board and 4x preview; reopened all sources.')
