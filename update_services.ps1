# PowerShell script to help update services to use UnitOfWork pattern
# This script shows what needs to be updated in each service

$services = @(
    "MedicationService.cs",
    "LabTestService.cs", 
    "RadiologyService.cs",
    "DiagnosisService.cs",
    "SurgeryService.cs",
    "SharedLinkService.cs"
)

Write-Host "Services that need updating:"
foreach ($service in $services) {
    Write-Host "- $service"
}

Write-Host "`nRequired changes for each service:"
Write-Host "1. Add IUnitOfWork to constructor"
Write-Host "2. Replace CreateAsync with Add + await _unitOfWork.CompleteAsync()"
Write-Host "3. Replace UpdateAsync with Update + await _unitOfWork.CompleteAsync()"
Write-Host "4. Replace DeleteAsync with Delete + await _unitOfWork.CompleteAsync()"
