$path = "c:\Users\Blessing Mwale\Documents\Projects\LUSE\Gateway\LuseGateway\LuseGateway.Fix\FIX50SP2.xml"
$content = [System.IO.File]::ReadAllText($path)

$newCF = "   <message name='PartyDetailsListRequest' msgtype='CF' msgcat='app'>
    <field name='PartyDetailsListRequestID' required='Y' />
    <group name='NoPartyListResponseTypes' required='Y'>
     <field name='PartyListResponseType' required='Y' />
    </group>
   </message>"

$newCG = "   <message name='PartyDetailsListReport' msgtype='CG' msgcat='app'>
    <field name='PartyDetailsListReportID' required='Y' />
    <field name='LastFragment' required='Y' />
    <group name='NoPartyIDs' required='Y'>
     <field name='PartyID' required='Y' />
     <field name='PartyIDSource' required='Y' />
     <field name='PartyRole' required='Y' />
     <group name='NoPartyDetailAltID' required='N'>
      <field name='PartyDetailAltID' required='N' />
     </group>
     <group name='NoRelatedPartyDetailID' required='N'>
      <field name='RelatedPartyDetailID' required='N' />
      <field name='RelatedPartyDetailRole' required='N' />
     </group>
    </group>
   </message>"

# Replace CF
$content = $content -replace "(?s)   <message name='PartyDetailsListRequest' msgtype='CF' msgcat='app'>.*?</message>", $newCF

# Replace CG
$content = $content -replace "(?s)   <message name='PartyDetailsListReport' msgtype='CG' msgcat='app'>.*?</message>", $newCG

[System.IO.File]::WriteAllText($path, $content)
Write-Host "FIX50SP2.xml updated successfully."
