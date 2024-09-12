import axios from "axios";

export async function pingServer(apiUrl: string): Promise<boolean> {
  try {
    await axios.get(apiUrl + "/pingServer");
    return true;
  } catch {
    console.error("Server not available");
    return false;
  }
}

export async function startBrowserInstance(apiUrl: string): Promise<boolean> {
  try {
    await axios.get(apiUrl + "/startBrowser");
    return true;
  } catch {
    console.error("Server browser instance could not be initialized");
    return false;
  }
}

export async function downloadImage(
  apiUrl: string
): Promise<{ imageName: string; imageBlob: string } | undefined> {
  try {
    const response = await axios.get(apiUrl + "/downloadImage", {
      responseType: "blob",
    });

    const imageName = response.headers["x-gt-image-name"];

    return {
      imageName: imageName,
      imageBlob: URL.createObjectURL(response.data),
    };
  } catch {
    console.error("Could not be download image from server");
  }
}

export async function fillInForm(
  apiUrl: string,
  data: {
    address: string;
    isMpImage: boolean;
    isHwImage: boolean;
    isBadImage: boolean;
  }
) {
  try {
    await axios.post(apiUrl + "/fillInForm", data);
  } catch {
    console.error("Could not fill in form data on the gt server");
  }
}

export async function gotoNextImage(
  apiUrl: string
): Promise<{ imageName: string; imageBlob: string } | undefined> {
  try {
    // Navigate to new image
    await axios.get(apiUrl + "/gotoNextImage");

    // Download and return image on the current page
    return await downloadImage(apiUrl);
  } catch {
    console.error("Could not navigate to or download the next image");
  }
}
