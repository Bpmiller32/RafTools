<template></template>;

import { defineComponent, onMounted, ref } from "vue";
import {
  BookmarkSquareIcon,
  ClipboardDocumentListIcon,
  ForwardIcon,
} from "@heroicons/vue/16/solid";

export default defineComponent({
  setup() {
    /* -------------------------------------------------------------------------- */
    /*                                    State                                   */
    /* -------------------------------------------------------------------------- */
    const textAreaRef = ref();

    const isMpImage = ref(false);
    const isHWImage = ref(false);
    const isBadImage = ref(false);

    const isRts = ref(false);
    const isFwd = ref(false);
    const is3547 = ref(false);

    /* -------------------------------------------------------------------------- */
    /*                         Mounting and watchers setup                        */
    /* -------------------------------------------------------------------------- */
    onMounted(() => {});

    /* -------------------------------------------------------------------------- */
    /*                                   Events                                   */
    /* -------------------------------------------------------------------------- */
    const CopyToClipBoardButtonClicked = async () => {
      const textToCopy = textAreaRef.value.value;

      try {
        await navigator.clipboard.writeText(textToCopy);
        // console.log("Text copied to clipboard!");
      } catch (err) {
        console.error("Failed to copy text to clipboard: ", err);
      }
    };

    const MailTypeButtonClicked = (buttonType: string) => {
      console.log(buttonType);

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

    const NavButtonClicked = (buttonType: string) => {
      if (buttonType === "Save") {
        const data = {
          address: "form 3547" + "\r" + textAreaRef.value.value,
          isMpImage: isMpImage.value,
        };

        // Put POST request here
        console.log(data);
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
            "flex items-center py-2 px-3 gap-2 border border-white/50": true,
            "rounded-l-xl": roundLeftCorner,
            "rounded-r-xl": roundRightCorner,
          }}
        >
          <div
            class={{
              "h-5 w-5 rounded-full": true,
              "bg-green-500 ring-1 ring-white":
                buttonType !== "Bad" && buttonVariable,
              "bg-red-500 ring-1 ring-white":
                buttonType === "Bad" && buttonVariable,
              "ring-1 ring-white": !buttonVariable,
            }}
          />
          <p class="text-white text-sm">{buttonType}</p>
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
            "flex items-center py-2 px-3 gap-2 border border-white/50": true,
            "rounded-l-xl": roundLeftCorner,
            "rounded-r-xl": roundRightCorner,
          }}
        >
          {NavButtonHelper(buttonType)}
          <p class="text-white text-sm">{buttonType}</p>
        </button>
      );
    };

    const NavButtonHelper = (buttonType: string) => {
      if (buttonType === "Save") {
        return <BookmarkSquareIcon class="h-5 w-5 text-gray-100" />;
      }
      if (buttonType === "Next") {
        return <ForwardIcon class="h-5 w-5 text-gray-100" />;
      }
    };

    /* -------------------------------------------------------------------------- */
    /*                               Render function                              */
    /* -------------------------------------------------------------------------- */
    return () => (
      <main class="overflow-hidden pt-5 pl-5">
        {/* Filename, textarea, clipboard copy button */}
        <section>
          <div class="flex items-center gap-10">
            <label
              for="comment"
              class="self-end block font-medium leading-6 text-gray-100 text-xs"
            >
              20240703_161418_9316_43616_01.jpg
            </label>
            <button
              onClick={CopyToClipBoardButtonClicked}
              class="flex items-center rounded-md border border-white/50 px-4 py-2 text-gray-100 text-sm"
            >
              <ClipboardDocumentListIcon class="mr-2 h-4 w-4 text-gray-100" />
              <p>Copy to Clipboard</p>
            </button>
          </div>
          <div class="mt-2">
            <textarea
              ref={textAreaRef}
              rows="4"
              name="comment"
              id="guiTextArea"
              class="bg-transparent text-gray-100 block resize-none w-full rounded-md border-0 py-1.5 shadow-sm ring-1 ring-inset ring-gray-300 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-indigo-600 sm:text-sm sm:leading-6"
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
            {NavButton("Save", true, false)}
            {NavButton("Next", false, true)}
          </div>
        </section>

        {/* Special mail designations */}
        <section class="mt-2 flex justify-center items-center">
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
