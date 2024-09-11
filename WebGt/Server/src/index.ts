import express, { Request, Response } from "express";
import bodyParser from "body-parser";
import cors from "cors";
import { Browser, Page } from "playwright";
import {
  downloadImage,
  fillInForm,
  getImageFromDisk,
  getImageName,
  gotoNextImage,
  manualGotoImage,
  startBrowser,
  stopBrowser,
} from "./playwright";

/* -------------------------------------------------------------------------- */
/*                                    Setup                                   */
/* -------------------------------------------------------------------------- */
// Expressjs fields
const app = express();
const port = 3000;

// Middleware to parse JSON bodies
app.use(bodyParser.json());

// Configure CORS
app.use(
  cors({
    origin: "http://localhost:5173", // Replace with approved frontend URLs
    methods: ["GET", "POST"],
    allowedHeaders: ["Content-Type"],
  })
);

// Playwright fields
let browser: Browser;
let page: Page;

/* -------------------------------------------------------------------------- */
/*                                  Requests                                  */
/* -------------------------------------------------------------------------- */

/* --------------------------- Test echo function --------------------------- */
app.get("/helloWorld", (req: Request, res: Response) => {
  res.status(200).send("Hello, World!");
});

/* ----------------------- Initialize browser instance ---------------------- */
app.get("/startBrowser", async (req: Request, res: Response) => {
  if (browser || page) {
    res.status(200).send("Browser instance already initialized and running");
    return;
  }

  try {
    // Call the async function and use temporary names for destructuring
    const pwOutput = await startBrowser(
      "http://groundtruth.raf.com/webgt/?l=en"
    );

    // Assign temporary variables to existing global variables
    browser = pwOutput.browser;
    page = pwOutput.page;

    res.status(200).send("Browser was initialized and logged in successfully");
  } catch (error) {
    res.status(500).send((error as Error).message);
  }
});

/* ------------------------ Destroy browser instance ------------------------ */
app.get("/stopBrowser", async (req: Request, res: Response) => {
  try {
    await stopBrowser(browser);

    res.status(200).send("Browser instance was closed successfully");
  } catch (error) {
    res.status(500).send((error as Error).message);
  }
});

/* ------- Get the current page's image name from the current page url ------ */
app.get("/getImageName", async (req: Request, res: Response) => {
  try {
    const pwOutput = await getImageName(page);

    res.status(200).send(pwOutput);
  } catch (error) {
    res.status(500).send((error as Error).message);
  }
});

/* --------- Download the image from the current page into Blob data -------- */
app.get("/downloadImage", async (req: Request, res: Response) => {
  try {
    const imageBuffer = await downloadImage(page);

    // Set the appropriate content type (e.g., image/jpeg, image/png)
    res.setHeader("Content-Type", "image/jpeg");

    // Set the response header with the image name to save a axios call from the FE
    const imageName = await getImageName(page);
    res.setHeader("X-Gt-Image-Name", imageName);

    // Send the image buffer
    res.status(200).send(imageBuffer);
  } catch (error) {
    res.status(500).send((error as Error).message);
  }
});

/* ------------- Fill in the gt form fields on the current page ------------- */
app.post("/fillInForm", async (req: Request, res: Response) => {
  try {
    // Extract data from the request body
    const requestData = req.body;

    await fillInForm(page, requestData);
    res.status(200).send("Form filled out successfully");
  } catch (error) {
    res.status(500).send((error as Error).message);
  }
});

/* ----------- Navigate to the next image using the page controls ----------- */
app.get("/gotoNextImage", async (req: Request, res: Response) => {
  try {
    await gotoNextImage(page);

    // // Delay the response to make sure next page/image finished loading and is present
    // setTimeout(() => {
    //   res
    //     .status(200)
    //     .send(
    //       "Next image is being displayed, this response was delayed by 1 second"
    //     );
    // }, 1000);

    res
      .status(200)
      .send("Page navigation control clicked, next image is being displayed");
  } catch (error) {
    res.status(500).send((error as Error).message);
  }
});

/* -------------------------------------------------------------------------- */
/*                               Debug requests                               */
/* -------------------------------------------------------------------------- */

/* --------------- Navigate and serve image's based on fileId --------------- */
app.post("/manualGotoImage", async (req: Request, res: Response) => {
  try {
    const requestData = req.body;
    await manualGotoImage(page, requestData);

    res
      .status(200)
      .send(
        "Page navigation manually executed by fileId, new image is being displayed"
      );
  } catch (error) {
    res.status(500).send((error as Error).message);
  }
});

/* ----- Get image from disk instead of gt, for debug and demonstration ----- */
app.get("/getImageFromDisk", async (req: Request, res: Response) => {
  try {
    const imageBuffer = await getImageFromDisk(
      "C:\\Users\\billym\\Desktop\\test.jpg"
    );

    // Set the appropriate content type (e.g., image/jpeg, image/png)
    res.setHeader("Content-Type", "image/jpeg");

    // Set the response header with the image name to save a axios call from the FE
    const imageName = await getImageName(page);
    res.setHeader("X-Gt-Image-Name", imageName);

    // Send the image buffer
    res.status(200).send(imageBuffer);
  } catch (error) {
    res.status(500).send((error as Error).message);
  }
});

/* -------------------------------------------------------------------------- */
/*                              Start the server                              */
/* -------------------------------------------------------------------------- */
app.listen(port, () => {
  console.log(`Server is running at http://localhost:${port}`);
});
