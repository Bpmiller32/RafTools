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

// Store refs
// const autoCrawlStatus = ref(store.crawlers[props.dirType].autoCrawlStatus);
// const autoCrawlEnabled = ref(store.crawlers[props.dirType].autoCrawlEnabled);
// const autoCrawlDate = ref(store.crawlers[props.dirType].autoCrawlDate);
const autoCrawlStatus = ref(null);
const autoCrawlEnabled = ref(null);
const autoCrawlDate = ref(null);

// Template refs
const refCrawlButtonIcon = ref(null);
const refEditPanelIcon = ref(null);

// States
const firstAnimationSupressed = ref(false);
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

    if (autoCrawlStatus.value == "Ready") {
      crawlButtonState.value.label = "Force Crawl";
      crawlButtonState.value.isActive = true;
      anime.remove(el);
      if (firstAnimationSupressed.value == false) {
        return;
      }
      crawlButtonState.value.animation = "ButtonFill";
    } else if (autoCrawlStatus.value == "In Progress") {
      crawlButtonState.value.label = "Crawling ....";
      crawlButtonState.value.isActive = false;
      animation.play();
      if (firstAnimationSupressed.value == false) {
        return;
      }
      crawlButtonState.value.animation = "ButtonDrain";
    } else if (autoCrawlStatus.value == "Error") {
      crawlButtonState.value.label = "Force Crawl";
      crawlButtonState.value.isActive = false;
      anime.remove(el);
      if (firstAnimationSupressed.value == false) {
        return;
      }
      crawlButtonState.value.animation = "ButtonDrain";
    } else if (autoCrawlStatus.value == "Disabled") {
      crawlButtonState.value.label = "Force Crawl";
      crawlButtonState.value.isActive = false;
      anime.remove(el);
      if (firstAnimationSupressed.value == false) {
        return;
      }
      crawlButtonState.value.animation = "ButtonDrain";
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

    if (newDate >= today) {
      store.SendMessageCrawler(
        props.dirType,
        "autoCrawlDate",
        dateString[1] + "/" + dateString[2] + "/" + dateString[0]
      );
      editPanelState.value.animation = "Flash";
      editPanelState.value.label = "Date updated";
    } else {
      editPanelState.value.animation = "HeadShake";
      editPanelState.value.label = "Invalid date";
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
    if (autoCrawlStatus.value == "Ready") {
      statusState.value.currentIcon = statusState.value.icons[0];
    } else if (autoCrawlStatus.value == "In Progress") {
      statusState.value.currentIcon = statusState.value.icons[1];
    } else if (autoCrawlStatus.value == "Error") {
      statusState.value.currentIcon = statusState.value.icons[2];
    } else if (autoCrawlStatus.value == "Disabled") {
      statusState.value.currentIcon = statusState.value.icons[3];
    }

    if (firstAnimationSupressed.value == false) {
      return;
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
  ],
  SetIcon: () => {
    if (props.dirType == "SmartMatch") {
      logoState.value.currentIcon = logoState.value.icons[0];
    } else if (props.dirType == "Parascript") {
      logoState.value.currentIcon = logoState.value.icons[1];
    } else if (props.dirType == "RoyalMail") {
      logoState.value.currentIcon = logoState.value.icons[2];
    } else {
      logoState.value.currentIcon = "Error";
    }
  },
});

// OnMounted functions
onMounted(() => {
  logoState.value.SetIcon();
  // statusState.value.SetState();
  // crawlButtonState.value.SetState();
  // await new Promise((resolve) => {
  //   setTimeout(console.log("timeout"), 5000);
  //   resolve();
  // });
  // editPanelState.value.MenuRotate;
});

// Watchers
// Deep watch to check store updates
watch(
  () => store.crawlers[props.dirType],
  () => {
    autoCrawlStatus.value = store.crawlers[props.dirType].autoCrawlStatus;
    autoCrawlEnabled.value = store.crawlers[props.dirType].autoCrawlEnabled;
    autoCrawlDate.value = store.crawlers[props.dirType].autoCrawlDate;

    // logoState.value.SetIcon();
    statusState.value.SetState();
    crawlButtonState.value.SetState();
    firstAnimationSupressed.value = true;
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
  if (crawlButtonState.value.isActive) {
    crawlButtonState.value.isActive = false;
    crawlButtonState.value.label = "Crawling ....";
    crawlButtonState.value.animation = "ButtonDrain";
    ClosePanel();

    // TODO: remove 2nd arg, move out of if statement
    store.SendMessageForceCrawl(props.dirType, "In Progress");
  } else {
    crawlButtonState.value.isActive = true;
    crawlButtonState.value.label = "Force Crawl";
    crawlButtonState.value.animation = "ButtonFill";

    // TODO: remove 2nd arg
    store.SendMessageForceCrawl(props.dirType, "Ready");
  }
  crawlButtonState.value.SetState();
}
function CheckboxClicked() {
  if (store.crawlers[props.dirType].autoCrawlEnabled == true) {
    store.SendMessageCrawler(props.dirType, "autoCrawlEnabled", false);
  } else {
    store.SendMessageCrawler(props.dirType, "autoCrawlEnabled", true);
  }
}
</script>

<template>
  <div
    class="overflow-hidden select-none min-w-[22rem] max-w-sm bg-white rounded-lg shadow divide-y divide-gray-200"
  >
    <div class="flex items-center justify-between p-6">
      <div class="shrink-0">
        <div class="flex items-center">
          <p class="text-gray-900 text-sm font-medium">
            {{ props.dirType }}
          </p>
          <AnimationHandler :animation="statusState.animation">
            <div
              :key="autoCrawlStatus"
              :class="{
                'text-green-800 bg-green-100': autoCrawlStatus == 'Ready',
                'text-yellow-800 bg-yellow-100':
                  autoCrawlStatus == 'In Progress',
                'text-red-800 bg-red-100': autoCrawlStatus == 'Error',
                'text-gray-800 bg-gray-100': autoCrawlStatus == 'Disabled',
                'ml-3 px-2 py-0.5 text-xs font-medium rounded-full': true,
              }"
            >
              {{ autoCrawlStatus }}
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
            v-model="autoCrawlEnabled"
            @click="CheckboxClicked()"
            class="flex items-center ml-2 cursor-pointer focus:ring-indigo-500 h-4 w-4 text-indigo-600 border-gray-300 rounded"
          />
        </div>
        <div class="text-gray-500 text-sm">
          <span>Next AutoCrawl: </span>
          <AnimationHandler :animation="statusState.animation">
            <span :key="autoCrawlDate" class="mr-16">{{ autoCrawlDate }}</span>
          </AnimationHandler>
        </div>
        <!-- <div class="bg-red-200 min-w-full pr-16">&nbsp;</div> -->
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
              <AnimationHandler :animation="editPanelState.animation">
                <p
                  :key="editPanelState.editDate"
                  class="mx-1 text-gray-500 mt-2 mb-4 t)ext-sm"
                >
                  {{ editPanelState.label }}
                </p>
              </AnimationHandler>
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
            @click="CrawlButtonClicked(close, $event)"
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
