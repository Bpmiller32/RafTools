function Generate-RandomPURCode {
    # Define the prefix
    $prefix = "PUR"

    # Generate a random number between 11 and 15 for the digit count
    $randomDigitCount = Get-Random -Minimum 3 -Maximum 5

    # Generate a string of random digits
    $randomDigits = -join (1..$randomDigitCount 	 ForEach-Object { Get-Random -Minimum 0 -Maximum 10 })

    # Return the result
    return $prefix + $randomDigits
}


for ($i = 0; $i -lt 25; $i++) {
    $randomPURCode = Generate-RandomPURCode
    Write-Output $randomPURCode
}