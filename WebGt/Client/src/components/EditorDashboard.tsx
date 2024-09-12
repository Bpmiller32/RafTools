import { defineComponent, onMounted, ref } from "vue";
import {
  ArrowUpCircleIcon,
  ArrowUturnLeftIcon,
  ForwardIcon,
  MagnifyingGlassCircleIcon,
  ScissorsIcon,
} from "@heroicons/vue/16/solid";
import Experience from "../webgl/experience";
import { fillInForm, gotoNextImage } from "./apiHandler";
import Emitter from "../webgl/utils/eventEmitter";

export default defineComponent({
  props: {
    apiUrl: {
      type: String,
      required: true,
    },
  },
  setup(props) {
    /* ---------------------------------- State --------------------------------- */
    const experience = Experience.getInstance();

    const imageNameRef = ref();
    const textAreaRef = ref();

    const isMpImage = ref(true); // Make this a default value
    const isHWImage = ref(false);
    const isBadImage = ref(false);

    const isRts = ref(false);
    const isFwd = ref(false);
    const is3547 = ref(false);
    const isDblFeed = ref(false);

    onMounted(() => {
      // TODO: fix this, experience and therefore events firing are not ready by the time this mounts
      setTimeout(() => {
        Emitter.on("fillInForm", async () => {
          await FormHelper();
        });
        Emitter.on("gotoNextImage", async () => {
          await NextImageHelper();
        });
      }, 1000);
    });

    /* --------------------------------- Events --------------------------------- */
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
          isDblFeed.value = false;
          break;

        case "FWD":
          isRts.value = false;
          isFwd.value = !isFwd.value;
          is3547.value = false;
          isDblFeed.value = false;
          break;

        case "Form 3547":
          isRts.value = false;
          isFwd.value = false;
          is3547.value = !is3547.value;
          isDblFeed.value = false;
          break;

        case "DBL Feed":
          isRts.value = false;
          isFwd.value = false;
          is3547.value = false;
          isDblFeed.value = !isDblFeed.value;
          break;

        default:
          break;
      }
    };

    const NavButtonClicked = (buttonType: string) => {
      if (buttonType === "Send") {
        Emitter.emit("fillInForm");
        return;
      }

      if (buttonType === "Next") {
        Emitter.emit("gotoNextImage");
        return;
      }
    };

    const ActionButtonClicked = (buttonType: string) => {
      switch (buttonType) {
        case "Cut":
          Emitter.emit("stitchBoxes");
          break;
        case "SendToVision":
          Emitter.emit("screenshotImage");
          break;
        case "Reset":
          Emitter.emit("resetImage");
          break;

        default:
          break;
      }
    };

    /* ---------------------------- Helper functions ---------------------------- */
    const FormHelper = async () => {
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
      if (isDblFeed.value) {
        data.address = "DBL FEED\n" + textAreaRef.value.value;
      }

      // Send POST request to server
      await fillInForm(props.apiUrl, data);
    };

    const NextImageHelper = async () => {
      // Navigate to the next image then download
      const image = await gotoNextImage(props.apiUrl);

      if (!image) {
        return;
      }

      // Start image load into webgl scene as a texture, resourceLoader will trigger an event when finished loading
      experience.resources.loadFromApi(image.imageBlob);

      // Set the image's name in the gui
      imageNameRef.value.innerText = image.imageName + ".jpg";

      // Clear all fields for new image, except isMpImage since that should be the default
      textAreaRef.value.value = "";
      isMpImage.value = true;
      isHWImage.value = false;
      isBadImage.value = false;
      isRts.value = false;
      isFwd.value = false;
      is3547.value = false;
      isDblFeed.value = false;
    };

    /* ------------------------------ Subcomponents ----------------------------- */
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
          {NavButtonIconSelector(buttonType)}
          <p class="text-white text-sm group-hover:text-indigo-100 duration-300">
            {buttonType}
          </p>
        </button>
      );
    };

    const NavButtonIconSelector = (buttonType: string) => {
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
          {ActionButtonIconSelector(buttonType)}
        </button>
      );
    };

    const ActionButtonIconSelector = (buttonType: string) => {
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

    /* ----------------------------- Render function ---------------------------- */
    return () => (
      <article class="overflow-hidden pt-5 pl-5">
        {/* Filename, textarea, clipboard copy button */}
        <section class="w-[27rem]">
          <div class="flex justify-between items-center">
            <label
              id="gtImageName"
              ref={imageNameRef}
              for="comment"
              class="mr-4 self-end overflow-hidden font-medium leading-6 text-gray-100 text-xs text-ellipsis"
            ></label>
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
            {MailTypeButton("Form 3547", is3547.value, false, false)}
            {MailTypeButton("DBL Feed", isDblFeed.value, false, true)}
          </div>
        </section>
      </article>
    );
  },
});
