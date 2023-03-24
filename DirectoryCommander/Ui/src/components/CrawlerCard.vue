<script setup>
import { onMounted, ref, watch } from "vue";
import { Disclosure, DisclosureButton, DisclosurePanel } from "@headlessui/vue";
import AnimationHandler from "./AnimationHandler.vue";
import anime from "animejs/lib/anime.es.js";
import {
  StatusOnlineIcon,
  ArrowCircleDownIcon,
  ExclamationCircleIcon,
  XCircleIcon,
  ChevronDoubleRightIcon,
  RefreshIcon,
} from "@heroicons/vue/outline";
import { useStore } from "../store";

const props = defineProps(["dirType"]);
const store = useStore();

// Template refs
const refCrawlButtonIcon = ref(null);
const refEditPanelIcon = ref(null);
const refEditPanelLabel = ref(null);

// States
const crawlButtonState = ref({
  isActive: null,
  label: null,
  animation: null,
  SetState: () => {
    const el = refCrawlButtonIcon.value;
    const animation = anime({
      targets: el,
      rotate: "-=2turn",
      easing: "easeInOutSine",
      loop: true,
      autoplay: false,
    });

    if (store.crawlers[props.dirType].DirectoryStatus == "Ready") {
      crawlButtonState.value.label = "Download";
      crawlButtonState.value.animation = "ButtonFill";
      anime.remove(el);
      crawlButtonState.value.isActive = true;
    } else if (store.crawlers[props.dirType].DirectoryStatus == "In Progress") {
      crawlButtonState.value.label = "Downloading ....";
      crawlButtonState.value.animation = "ButtonDrain";
      animation.play();
      crawlButtonState.value.isActive = false;
    } else if (store.crawlers[props.dirType].DirectoryStatus == "Error") {
      crawlButtonState.value.label = "Download";
      crawlButtonState.value.animation = "ButtonDrain";
      anime.remove(el);
      crawlButtonState.value.isActive = false;
    } else if (store.crawlers[props.dirType].DirectoryStatus == "Disabled") {
      crawlButtonState.value.label = "Download";
      crawlButtonState.value.animation = "ButtonDrain";
      anime.remove(el);
      crawlButtonState.value.isActive = false;
    }
  },
});
const editPanelState = ref({
  label: "Enter date",
  animation: null,
  editDate: null,
  RotateMenu: (isPanelOpen) => {
    const el = refEditPanelIcon.value;
    let rotationDirection = [];

    if (isPanelOpen == false) {
      rotationDirection = [0, 90];
    } else {
      rotationDirection = [90, 0];
    }
    anime({
      targets: el,
      rotate: rotationDirection,
      duration: 500,
      easing: "easeInOutSine",
    });
  },
  SetDate: () => {
    const dateString = editPanelState.value.editDate.split("-");
    const newDate = new Date(dateString[0], dateString[1] - 1, dateString[2]);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const el = refEditPanelLabel.value;

    if (newDate >= today) {
      store.SendMessageCrawler(
        props.dirType,
        "AutoDate",
        dateString[1] + "/" + dateString[2] + "/" + dateString[0]
      );
      editPanelState.value.label = "Date updated";

      anime({
        targets: el,
        keyframes: [
          {
            opacity: 1,
            color: "rgb(34, 197, 94)",
            duration: 0,
            easing: "easeInOutQuad",
          },
          {
            opacity: 0,
            duration: 250,
            easing: "easeInOutQuad",
          },
          {
            opacity: 1,
            duration: 250,
            easing: "easeInOutQuad",
          },
          {
            opacity: 0,
            duration: 250,
            easing: "easeInOutQuad",
          },
          {
            opacity: 0.99999,
            duration: 250,
            easing: "easeInOutQuad",
          },
        ],
      });
    } else {
      editPanelState.value.label = "Invalid date";

      anime({
        targets: el,
        keyframes: [
          {
            translateX: 0,
            color: "rgb(239, 68, 68)",
            duration: 0,
            easing: "easeInOutQuad",
          },
          {
            translateX: -6,
            rotateY: -9,
            duration: 65,
            easing: "easeInOutQuad",
          },
          {
            translateX: 5,
            rotateY: 7,
            duration: 120,
            easing: "easeInOutQuad",
          },
          {
            translateX: -3,
            rotateY: -5,
            duration: 130,
            easing: "easeInOutQuad",
          },
          {
            translateX: 2,
            rotateY: 3,
            duration: 120,
            easing: "easeInOutQuad",
          },
          {
            translateX: 0,
            duration: 65,
            easing: "easeInOutQuad",
          },
        ],
      });
    }
  },
});
const statusState = ref({
  currentIcon: null,
  icons: [
    StatusOnlineIcon,
    ArrowCircleDownIcon,
    ExclamationCircleIcon,
    XCircleIcon,
  ],
  animation: null,
  SetState: () => {
    if (store.crawlers[props.dirType].DirectoryStatus == "Ready") {
      statusState.value.currentIcon = statusState.value.icons[0];
    } else if (store.crawlers[props.dirType].DirectoryStatus == "In Progress") {
      statusState.value.currentIcon = statusState.value.icons[1];
    } else if (store.crawlers[props.dirType].DirectoryStatus == "Error") {
      statusState.value.currentIcon = statusState.value.icons[2];
    } else if (store.crawlers[props.dirType].DirectoryStatus == "Disabled") {
      statusState.value.currentIcon = statusState.value.icons[3];
    }

    statusState.value.animation = "FadeIn";
  },
});
const logoState = ref({
  currentIcon: null,
  icons: [
    new URL("../assets/SmartMatchLogo.png", import.meta.url).href,
    new URL("../assets/ParascriptLogo.png", import.meta.url).href,
    new URL("../assets/RoyalMailLogo.png", import.meta.url).href,
    new URL("../assets/ErrorLogo.png", import.meta.url).href,
  ],
  SetIcon: () => {
    if (props.dirType == "SmartMatch") {
      logoState.value.currentIcon = logoState.value.icons[0];
    } else if (props.dirType == "Parascript") {
      logoState.value.currentIcon = logoState.value.icons[1];
    } else if (props.dirType == "RoyalMail") {
      logoState.value.currentIcon = logoState.value.icons[2];
    } else {
      logoState.value.currentIcon = logoState.value.icons[3];
    }
  },
});

// OnMounted
onMounted(() => {
  logoState.value.SetIcon();
  statusState.value.SetState();
  crawlButtonState.value.SetState();
});

// Watchers
// Deep watch to check store changes
watch(
  () => store.crawlers[props.dirType],
  () => {
    statusState.value.SetState();
    crawlButtonState.value.SetState();
  },
  { deep: true }
);
// To validate date
watch(
  () => editPanelState.value.editDate,
  () => editPanelState.value.SetDate()
);

// Events
function PanelButtonClicked(isPanelOpen, ClosePanel) {
  if (crawlButtonState.value.isActive == false) {
    ClosePanel();
    return;
  }
  editPanelState.value.label = "Enter date";
  editPanelState.value.RotateMenu(isPanelOpen);
}
function CrawlButtonClicked(ClosePanel) {
  if (crawlButtonState.value.isActive == false) {
    return;
  }

  ClosePanel();
  store.SendMessageCrawler(props.dirType, "Force", "Force");
}
function CheckboxClicked() {
  if (crawlButtonState.value.isActive == false) {
    return;
  }

  if (store.crawlers[props.dirType].AutoEnabled == true) {
    store.SendMessageCrawler(props.dirType, "AutoEnabled", "false");
  } else {
    store.SendMessageCrawler(props.dirType, "AutoEnabled", "true");
  }
}
</script>

<template>
  <div
    class="overflow-hidden select-none min-w-[23rem] max-w-[23rem] bg-white rounded-lg shadow divide-y divide-gray-200"
  >
    <div class="flex items-center justify-between p-6">
      <div class="shrink-0">
        <div class="flex items-center">
          <p class="text-gray-900 text-sm font-medium">
            {{ props.dirType }}
          </p>
          <AnimationHandler :animation="statusState.animation">
            <div
              :key="store.crawlers[props.dirType].DirectoryStatus"
              :class="{
                'text-green-800 bg-green-100':
                  store.crawlers[props.dirType].DirectoryStatus == 'Ready',
                'text-yellow-800 bg-yellow-100':
                  store.crawlers[props.dirType].DirectoryStatus ==
                  'In Progress',
                'text-red-800 bg-red-100':
                  store.crawlers[props.dirType].DirectoryStatus == 'Error',
                'text-gray-800 bg-gray-100':
                  store.crawlers[props.dirType].DirectoryStatus == 'Disabled',
                'ml-3 px-2 py-0.5 text-xs font-medium rounded-full': true,
              }"
            >
              {{ store.crawlers[props.dirType].DirectoryStatus }}
            </div>
          </AnimationHandler>
          <AnimationHandler :animation="statusState.animation">
            <component
              :is="statusState.currentIcon"
              :class="{
                'text-green-500':
                  statusState.currentIcon == statusState.icons[0],
                'text-yellow-500':
                  statusState.currentIcon == statusState.icons[1],
                'text-red-500': statusState.currentIcon == statusState.icons[2],
                'text-gray-500':
                  statusState.currentIcon == statusState.icons[3],
                'h-5 w-5 ml-1': true,
              }"
            ></component>
          </AnimationHandler>
        </div>
        <div class="flex items-center mt-2 text-gray-500 text-sm">
          <p>AutoCrawl:</p>
          <input
            type="checkbox"
            v-model="store.crawlers[props.dirType].AutoEnabled"
            @click="CheckboxClicked()"
            :disabled="!crawlButtonState.isActive"
            :class="{
              'text-indigo-600 cursor-pointer':
                crawlButtonState.isActive == true,
              'text-gray-400 cursor-not-allowed':
                crawlButtonState.isActive == false,
              'flex items-center ml-2 h-4 w-4 focus:ring-indigo-500 border-gray-300 rounded disabled:bg-gray-400': true,
            }"
          />
        </div>
        <div class="text-gray-500 text-sm">
          <span>Next AutoCrawl: </span>
          <AnimationHandler :animation="statusState.animation">
            <span :key="store.crawlers[props.dirType].AutoDate" class="mr-16">{{
              store.crawlers[props.dirType].AutoDate
            }}</span>
          </AnimationHandler>
        </div>
      </div>
      <img class="w-20 h-20 border rounded-full" :src="logoState.currentIcon" />
    </div>
    <Disclosure
      as="div"
      v-slot="{ open, close }"
      class="flex justify-between divide-x divide-gray-200"
    >
      <div class="flex flex-1 justify-center">
        <div class="shrink-0">
          <AnimationHandler
            :animation="crawlButtonState.animation"
            args="panelbutton"
          >
            <DisclosureButton
              :key="crawlButtonState.isActive"
              @click="PanelButtonClicked(open, close)"
              :class="{
                'bg-indigo-100 text-indigo-700':
                  crawlButtonState.isActive == true,
                'bg-gray-500 text-white bg-[length:0%,0%] cursor-not-allowed':
                  crawlButtonState.isActive == false,
                'bg-[length:0%,0%] bg-indigo-200 hover:bg-indigo-100':
                  crawlButtonState.isActive == true && open == true,
                'bg-[length:0%,0%] bg-indigo-100 hover:bg-indigo-200':
                  crawlButtonState.isActive == true && open == false,
                'flex items-center mx-5 my-4 px-2 py-2 max-h-8 bg-gradient-to-r from-indigo-100 to-indigo-100 bg-no-repeat bg-center border border-transparent text-sm leading-4 font-medium rounded-md focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500': true,
              }"
              ><ChevronDoubleRightIcon
                ref="refEditPanelIcon"
                class="h-5 w-5 mr-1"
              />Edit AutoCrawl
            </DisclosureButton>
          </AnimationHandler>
          <AnimationHandler animation="SlideDown">
            <DisclosurePanel class="overflow-hidden h-0 ml-2">
              <label class="mx-1 text-sm font-medium text-gray-700"
                >New AutoCrawl Date</label
              >
              <div class="mt-1">
                <input
                  type="date"
                  v-model="editPanelState.editDate"
                  class="mx-1 px-2 shadow-sm focus:ring-indigo-500 focus:border-indigo-500 border-gray-300 rounded-md"
                  placeholder="MM/DD/YYYY"
                />
              </div>
              <p
                ref="refEditPanelLabel"
                class="mx-1 text-gray-500 mt-2 mb-4 t)ext-sm"
              >
                {{ editPanelState.label }}
              </p>
            </DisclosurePanel>
          </AnimationHandler>
        </div>
      </div>
      <div class="flex flex-1 justify-center items-center -space-x-7">
        <RefreshIcon
          ref="refCrawlButtonIcon"
          class="shrink-0 h-5 w-5 text-white z-10"
        />
        <AnimationHandler :animation="crawlButtonState.animation">
          <button
            type="button"
            :key="crawlButtonState.isActive"
            @click="CrawlButtonClicked(close)"
            :class="{
              'bg-indigo-600 bg-[length:150%,150%] hover:bg-[length:0%,0%] hover:bg-indigo-700':
                crawlButtonState.isActive == true,
              'bg-gray-500 bg-[length:0%,0%] cursor-not-allowed':
                crawlButtonState.isActive == false,
              'flex shrink-0 items-center my-4 pl-8 pr-2 py-2 max-h-8 bg-gradient-to-r from-indigo-600 to-indigo-600 bg-no-repeat bg-center border border-transparent text-sm leading-4 font-medium rounded-md shadow-sm text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500': true,
            }"
          >
            {{ crawlButtonState.label }}
          </button>
        </AnimationHandler>
      </div>
    </Disclosure>
  </div>
</template>
