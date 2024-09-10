import { Browser, chromium, Page } from "playwright";

export async function startBrowser(
  url: string
): Promise<{ browser: Browser; page: Page }> {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();

  // Navigate to the webpage
  await page.goto(url);

  /* -------------------------------------------------------------------------- */
  /*                                 Login Page                                 */
  /* -------------------------------------------------------------------------- */

  /* ----------------------------- Input username ----------------------------- */
  // Wait for the element to be visible
  const usernameSelector = "#gtuser";
  await page.waitForSelector(usernameSelector, { state: "visible" });

  // Focus on the text input
  await page.click(usernameSelector); // This will focus the input field

  // Clear the input field by setting an empty value
  await page.fill(usernameSelector, ""); // Clears the input field

  // Define the text to type into the input
  const username = "billym";

  // Type the text into the input
  await page.fill(usernameSelector, username);

  /* ----------------------------- Input password ----------------------------- */
  // Focus on the text input
  const passwordSelector = "#pass";
  await page.click(passwordSelector); // This will focus the input field

  // Clear the input field by setting an empty value
  await page.fill(passwordSelector, ""); // Clears the input field

  // Define the text to type into the input
  const password = "truth!bm";

  // Type the text into the input
  await page.fill(passwordSelector, password);

  /* ------------------------------ Input project ----------------------------- */
  const projectSelector = "#projectid";
  await page.click(projectSelector); // This will focus the input field

  // Clear the input field by setting an empty value
  await page.fill(projectSelector, ""); // Clears the input field

  // Define the text to type into the input
  const project = "USPS_NPI_Address";

  // Type the text into the input
  await page.fill(projectSelector, project);

  /* ---------------------------- Login to main app --------------------------- */
  const loginButtonSelector = "#login > input[type=submit]:nth-child(14)";
  await page.click(loginButtonSelector);

  /* -------------------------------------------------------------------------- */
  /*                                Main App Page                               */
  /* -------------------------------------------------------------------------- */
  const instructionsExit = "#instructionstextdiv";
  await page.waitForSelector(instructionsExit, { state: "visible" });

  await page.click(instructionsExit);

  return { browser: browser, page: page };
}

export async function stopBrowser(browser: Browser) {
  await browser.close();
}

export async function getImageName(page: Page): Promise<string | null> {
  const url = page.url();

  // Regular expression to match the fileid parameter
  const regex = /[?&]fileid=([^&]*)/;
  const match = url.match(regex);

  // If match is found, return the captured fileid, otherwise return null
  return match ? match[1] : null;
}

export async function downloadImage(page: Page): Promise<Buffer> {
  // Define image's selector
  const imageSelector = "#viewer > img";

  // Find the image element by its selector and get the image URL (relative or full)
  const relativeImageUrl = await page.getAttribute(imageSelector, "src"); // Replace with actual image selector

  // Working method, requires navigation
  if (!relativeImageUrl) {
    throw new Error("Could not find image on the page");
  }

  // Construct the full URL if necessary
  const baseUrl = "http://groundtruth.raf.com"; // Replace with the actual base URL of the site
  const fullImageUrl = relativeImageUrl.startsWith("http")
    ? relativeImageUrl
    : `${baseUrl}${relativeImageUrl}`;

  // Fetch the image
  const imageResponse = await page.goto(fullImageUrl);

  const buffer = await imageResponse!.body();

  // Debug: save image to disk
  // const imagePath = path.resolve("downloaded-image.png"); // Define desired file name and path
  // fs.writeFileSync(imagePath, buffer);
  // console.log(`Image downloaded to ${imagePath}`);

  // Navigate back to the previous page
  await page.goBack();

  return buffer;
}

export async function fillInForm(
  page: Page,
  requestData: {
    address: string;
    isMpImage: boolean;
    isHwImage: boolean;
    isBadImage: boolean;
  }
) {
  // Wait for the element to be visible
  const addressFieldSelector = "#destaddress";
  await page.waitForSelector(addressFieldSelector, { state: "visible" });

  // Fill in the textarea
  await page.fill(addressFieldSelector, requestData.address);

  // Check the appropriate imageType checkbox
  if (requestData.isMpImage) {
    const checkboxSelector = "#destmp";
    await page.waitForSelector(checkboxSelector, { state: "visible" });

    await page.check(checkboxSelector);
  }
  if (requestData.isHwImage) {
    const checkboxSelector = "#desthw";
    await page.waitForSelector(checkboxSelector, { state: "visible" });

    await page.check(checkboxSelector);
  }
  if (requestData.isBadImage) {
    const checkboxSelector = "#destbad";
    await page.waitForSelector(checkboxSelector, { state: "visible" });

    await page.check(checkboxSelector);
  }

  // Save the result
  await saveForm(page);
}

export async function saveForm(page: Page) {
  // Wait for the element to be visible
  const saveButtonSelector = "#save";
  await page.waitForSelector(saveButtonSelector, { state: "visible" });

  // Click on the next button
  await page.click(saveButtonSelector);
}

export async function gotoNextImage(page: Page) {
  // Wait for the element to be visible
  const nextButtonSelector = "#next";
  await page.waitForSelector(nextButtonSelector, { state: "visible" });

  // Click on the next button
  await page.click(nextButtonSelector);
}
