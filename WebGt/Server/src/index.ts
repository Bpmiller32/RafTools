import express, { Request, Response } from "express";
import cors from "cors"; // Import cors
import bodyParser from "body-parser";
import { Browser, Page } from "playwright";
import {
  downloadImage,
  fillInForm,
  getImageName,
  gotoNextImage,
  startBrowser,
  stopBrowser,
} from "./playwright";

/* -------------------------------------------------------------------------- */
/*                                    Setup                                   */
/* -------------------------------------------------------------------------- */
const app = express();
const port = 3000;

let browser: Browser;
let page: Page;

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

/* -------------------------------------------------------------------------- */
/*                                  Requests                                  */
/* -------------------------------------------------------------------------- */
app.get("/helloWorld", (req: Request, res: Response) => {
  res.send("Hello, World!");
});

app.get("/startBrowser", async (req: Request, res: Response) => {
  try {
    // Call the async function and use temporary names for destructuring
    const { browser: funcBrowser, page: funcPage } = await startBrowser(
      "http://groundtruth.raf.com/webgt/?l=en"
    );

    // Assign temporary variables to existing variables
    browser = funcBrowser;
    page = funcPage;

    res.send("Browser was started and logged in successfully");
  } catch (error) {
    console.error(error);
  }
});

app.get("/stopBrowser", async (req: Request, res: Response) => {
  try {
    await stopBrowser(browser);

    res.send("Browser was closed successfully");
  } catch (error) {
    console.error(error);
  }
});

app.get("/getImageName", async (req: Request, res: Response) => {
  try {
    const response = await getImageName(page);

    res.send(response);
  } catch (error) {
    console.error(error);
  }
});

app.get("/downloadImage", async (req: Request, res: Response) => {
  try {
    const imageBuffer = await downloadImage(page);

    // Set the appropriate content type (e.g., image/jpeg, image/png)
    res.setHeader("Content-Type", "image/jpeg"); // Change this based on image format

    // Send the image buffer
    res.send(imageBuffer);
  } catch (error) {
    console.error(error);
  }
});

app.post("/fillInForm", async (req: Request, res: Response) => {
  try {
    // Extract data from the request body
    const requestData = req.body;

    await fillInForm(page, requestData);
    res.send("Form filled out successfully");
  } catch (error) {
    console.error(error);
  }
});

app.get("/gotoNextImage", async (req: Request, res: Response) => {
  try {
    await gotoNextImage(page);

    // Delay the response to make sure on the next page/image
    setTimeout(() => {
      res.send(
        "Next image is being displayed, this response was delayed by 1 second"
      );
    }, 1000);
  } catch (error) {
    console.error(error);
  }
});

/* -------------------------------------------------------------------------- */
/*                              Start the server                              */
/* -------------------------------------------------------------------------- */
app.listen(port, () => {
  console.log(`Server is running at http://localhost:${port}`);
});
