import { defineComponent, ref } from "vue";
import {
  ArrowUpCircleIcon,
  ArrowUturnLeftIcon,
  ForwardIcon,
  MagnifyingGlassCircleIcon,
  ScissorsIcon,
} from "@heroicons/vue/16/solid";
import Experience from "../webgl/experience";
import axios from "axios";

export default defineComponent({
  setup() {
    /* -------------------------------------------------------------------------- */
    /*                                    State                                   */
    /* -------------------------------------------------------------------------- */
    const experience = Experience.getInstance();

    const imageNameRef = ref();
    const textAreaRef = ref();

    const isMpImage = ref(false);
    const isHWImage = ref(false);
    const isBadImage = ref(false);

    const isRts = ref(false);
    const isFwd = ref(false);
    const is3547 = ref(false);

    /* -------------------------------------------------------------------------- */
    /*                                   Events                                   */
    /* -------------------------------------------------------------------------- */
    // const CopyToClipBoardButtonClicked = async () => {
    //   const textToCopy = textAreaRef.value.value;

    //   try {
    //     await navigator.clipboard.writeText(textToCopy);
    //     // console.log("Text copied to clipboard!");
    //   } catch (err) {
    //     console.error("Failed to copy text to clipboard: ", err);
    //   }
    // };

    const MailTypeButtonClicked = (buttonType: string) => {
      switch (buttonType) {
        case "MP":
          isMpImage.value = !isMpImage.value;
          isHWImage.value = false;
          isBadImage.value = false;
          break;

        case "HW":
          isMpImage.value = false;
          isHWImage.value = !isHWImage.value;
          isBadImage.value = false;
          break;

        case "Bad":
          isMpImage.value = false;
          isHWImage.value = false;
          isBadImage.value = !isBadImage.value;
          break;

        case "RTS/RFS":
          isRts.value = !isRts.value;
          isFwd.value = false;
          is3547.value = false;
          break;

        case "FWD":
          isRts.value = false;
          isFwd.value = !isFwd.value;
          is3547.value = false;
          break;

        case "Form 3547":
          isRts.value = false;
          isFwd.value = false;
          is3547.value = !is3547.value;
          break;

        default:
          break;
      }
    };

    const NavButtonClicked = async (buttonType: string) => {
      if (buttonType === "Send") {
        // Define data request body
        const data = {
          address: textAreaRef.value.value,

          isMpImage: isMpImage.value,
          isHwImage: isHWImage.value,
          isBadImage: isBadImage.value,
        };

        // Change the data based on gui
        if (isRts.value) {
          data.address = "RTS\n" + textAreaRef.value.value;
        }
        if (isFwd.value) {
          data.address = "FWD\n" + textAreaRef.value.value;
        }
        if (is3547.value) {
          data.address = "FORM3547\n" + textAreaRef.value.value;
        }

        // Define the API endpoint URL
        const apiUrl = "https://termite-grand-moose.ngrok-free.app/fillInForm";

        // Send POST request with Axios
        try {
          const response = await axios.post(apiUrl, data);
          console.log(response);
        } catch (error) {
          console.error("Error:", error);
        }

        return;
      }

      if (buttonType === "Next") {
        // Define the API endpoint URL
        const nextImageUrl =
          "https://termite-grand-moose.ngrok-free.app/gotoNextImage";
        const getImageName =
          "https://termite-grand-moose.ngrok-free.app/getImageName";
        const downloadImageUrl =
          "https://termite-grand-moose.ngrok-free.app/downloadImage";

        // Navigate to new image
        try {
          await axios.get(nextImageUrl);
        } catch (error) {
          console.error(error);
        }

        // Clear all fields for new image
        textAreaRef.value.value = "";
        isMpImage.value = false;
        isHWImage.value = false;
        isBadImage.value = false;
        isRts.value = false;
        isFwd.value = false;
        is3547.value = false;

        // Get new image's name, set in gui
        try {
          const response = await axios.get(getImageName);
          imageNameRef.value.innerText = response.data + ".jpg";
        } catch (error) {
          console.error(error);
        }

        // Pull new image
        try {
          const response = await axios.get(downloadImageUrl, {
            responseType: "arraybuffer",
          });

          // Convert response data to a Blob
          const imageBlob = new Blob([response.data], {
            type: response.headers["content-type"],
          });

          // Create a URL for the Blob
          const imageUrl = URL.createObjectURL(imageBlob);

          // // Debug: download image to disk
          // const a = document.createElement("a");
          // a.href = imageUrl;
          // a.download = "debugResponseImage.jpg";

          // // Append the <a> element to the body (necessary for Firefox)
          // document.body.appendChild(a);

          // // Trigger the download
          // a.click();

          // Start load into three as a texture, event handler will trigger when finished in three\world
          experience.resources.startLoadingFromApi(imageUrl);

          // Clean up
          URL.revokeObjectURL(imageUrl);
        } catch (error) {
          console.error(error);
        }

        return;
      }
    };

    const ActionButtonClicked = (buttonType: string) => {
      switch (buttonType) {
        case "Cut":
          experience.input.emit("stitchBoxes");
          break;
        case "SendToVision":
          experience.input.emit("screenshotImage");
          break;
        case "Reset":
          experience.input.emit("resetImage");
          break;

        default:
          break;
      }
    };

    /* -------------------------------------------------------------------------- */
    /*                                Subcomponents                               */
    /* -------------------------------------------------------------------------- */
    const MailTypeButton = (
      buttonType: string,
      buttonVariable: boolean,
      roundLeftCorner: boolean,
      roundRightCorner: boolean
    ) => {
      return (
        <button
          onClick={() => MailTypeButtonClicked(buttonType)}
          class={{
            "flex items-center py-2 px-3 gap-2 border border-white/50 group hover:border-indigo-600 duration-300":
              true,
            "rounded-l-xl": roundLeftCorner,
            "rounded-r-xl": roundRightCorner,
          }}
        >
          <div
            class={{
              "h-5 w-5 rounded-full duration-300": true,
              "bg-green-500 ring-1 ring-white":
                buttonType !== "Bad" && buttonVariable,
              "bg-red-500 ring-1 ring-white":
                buttonType === "Bad" && buttonVariable,
              "ring-1 ring-white": !buttonVariable,
            }}
          />
          <p class="text-white text-sm group-hover:text-indigo-200 duration-300">
            {buttonType}
          </p>
        </button>
      );
    };

    const NavButton = (
      buttonType: string,
      roundLeftCorner: boolean,
      roundRightCorner: boolean
    ) => {
      return (
        <button
          onClick={() => NavButtonClicked(buttonType)}
          class={{
            "flex items-center py-2 px-3 gap-2 border border-white/50 group hover:border-indigo-600 duration-300":
              true,
            "rounded-l-xl": roundLeftCorner,
            "rounded-r-xl": roundRightCorner,
          }}
        >
          {NavButtonHelper(buttonType)}
          <p class="text-white text-sm group-hover:text-indigo-100 duration-300">
            {buttonType}
          </p>
        </button>
      );
    };

    const NavButtonHelper = (buttonType: string) => {
      if (buttonType === "Send") {
        return (
          <ArrowUpCircleIcon class="h-5 w-5 text-gray-100 transition-colors group-hover:text-indigo-100 duration-300" />
        );
      }
      if (buttonType === "Next") {
        return (
          <ForwardIcon class="h-5 w-5 text-gray-100 transition-colors group-hover:text-indigo-100 duration-300" />
        );
      }
    };

    const ActionButton = (
      buttonType: string,
      roundLeftCorner: boolean,
      roundRightCorner: boolean
    ) => {
      return (
        <button
          onClick={() => ActionButtonClicked(buttonType)}
          class={{
            "py-2 px-3 border border-white/50 transition-colors group hover:border-indigo-600 duration-300":
              true,
            "rounded-l-xl": roundLeftCorner,
            "rounded-r-xl": roundRightCorner,
          }}
        >
          {ActionButtonHelper(buttonType)}
        </button>
      );
    };

    const ActionButtonHelper = (buttonType: string) => {
      switch (buttonType) {
        case "Cut":
          return (
            <ScissorsIcon class="h-5 w-5 text-gray-100 group-hover:text-indigo-100 duration-300" />
          );

        case "SendToVision":
          return (
            <MagnifyingGlassCircleIcon class="h-5 w-5 text-gray-100 group-hover:text-indigo-100 duration-300" />
          );

        case "Reset":
          return (
            <ArrowUturnLeftIcon class="h-5 w-5 text-gray-100 group-hover:text-indigo-100 duration-300" />
          );

        default:
          break;
      }
    };

    /* -------------------------------------------------------------------------- */
    /*                               Render function                              */
    /* -------------------------------------------------------------------------- */
    return () => (
      <main class="overflow-hidden pt-5 pl-5">
        {/* Filename, textarea, clipboard copy button */}
        <section class="w-[27rem]">
          <div class="flex justify-between items-center">
            <label
              ref={imageNameRef}
              for="comment"
              class="mr-4 self-end overflow-hidden font-medium leading-6 text-gray-100 text-xs text-ellipsis"
            >
              20240703_161418_9316_43616_01.jpg
            </label>
            <div class="flex">
              {NavButton("Send", true, false)}
              {NavButton("Next", false, true)}
            </div>
          </div>
          <div class="mt-2">
            <textarea
              ref={textAreaRef}
              rows="4"
              id="guiTextArea"
              class="bg-transparent text-gray-100 text-sm leading-6 resize-none w-full rounded-md border-0 py-1.5 shadow-sm ring-1 ring-inset ring-gray-300 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-indigo-600"
            />
          </div>
        </section>

        {/* Mail type and nav buttons */}
        <section class="mt-2 flex justify-between items-center">
          <div class="flex">
            {MailTypeButton("MP", isMpImage.value, true, false)}
            {MailTypeButton("HW", isHWImage.value, false, false)}
            {MailTypeButton("Bad", isBadImage.value, false, true)}
          </div>
          <div class="flex">
            {ActionButton("Cut", true, false)}
            {ActionButton("SendToVision", false, false)}
            {ActionButton("Reset", false, true)}
          </div>
        </section>

        {/* Special mail designations */}
        <section class="mt-2 flex items-center">
          <div class="flex">
            {MailTypeButton("RTS/RFS", isRts.value, true, false)}
            {MailTypeButton("FWD", isFwd.value, false, false)}
            {MailTypeButton("Form 3547", is3547.value, false, true)}
          </div>
        </section>
      </main>
    );
  },
});
