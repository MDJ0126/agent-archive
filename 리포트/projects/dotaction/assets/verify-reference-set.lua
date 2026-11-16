local root=app.params.root or 'C:/Projects/DotAction'
local pc=app.pixelColor
local allowed={}
local palette=Palette{fromFile=root..'/art/palettes/dotaction-32.gpl'}
for i=0,#palette-1 do local c=palette:getColor(i);allowed[pc.rgba(c.red,c.green,c.blue,255)]=true end
local specs={
 {'characters/player','chr_player',32,32}, {'characters/enemies','chr_enemy_scrapper',32,32},
 {'symbols','sym_blade',16,16},{'symbols','sym_bolt',16,16},{'symbols','sym_guard',16,16},
 {'ui','ui_slot_cell',32,32},{'cards','ui_card_frame',96,128},{'environment','tile_floor',16,16}
}
local total=0
for _,spec in ipairs(specs) do
 local s=app.open(root..'/art/source/'..spec[1]..'/'..spec[2]..'.aseprite')
 assert(s.width==spec[3] and s.height==spec[4],spec[2]..': dimensions')
 assert(not s.isModified,spec[2]..': unsaved changes')
 local im=Image{fromFile=root..'/art/exports/'..spec[1]..'/'..spec[2]..'.png'}
 assert(im.width==spec[3] and im.height==spec[4],spec[2]..': PNG dimensions')
 local count=0
 for y=0,im.height-1 do for x=0,im.width-1 do
  local p=im:getPixel(x,y);local a=pc.rgbaA(p)
  assert(a==0 or a==255,spec[2]..': partial alpha')
  if a==255 then
   assert(allowed[p],spec[2]..': off-palette pixel');count=count+1
   if spec[1]=='symbols' then assert(x>=2 and x<=13 and y>=2 and y<=13,spec[2]..': symbol padding') end
  end
 end end
 assert(count>0,spec[2]..': empty artwork')
 total=total+count
 print(spec[2]..' OK '..im.width..'x'..im.height..' opaque='..count..' layers='..#s.layers)
 s:close()
end
local out=io.open(root..'/art/previews/verification.json','w')
out:write('{"tool":"Aseprite 1.3.18.6 Lua API","assets":8,"nativeReopen":true,"dimensions":true,"palette32":true,"binaryAlpha":true,"symbolPadding":true,"opaquePixels":'..total..',"visualApproval":"draft"}\n');out:close()
print('ART_VERIFICATION_OK')
