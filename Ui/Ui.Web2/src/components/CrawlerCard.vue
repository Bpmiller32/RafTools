<script setup>
import { ref, watch, watchEffect, watchPostEffect } from "vue";
import { Disclosure, DisclosureButton, DisclosurePanel } from "@headlessui/vue";
import AnimationHandler from "./AnimationHandler.vue";
import CrawlerList from "./CrawlerList.vue";
import anime from "animejs/lib/anime.es.js";
import {
  StatusOnlineIcon,
  ArrowCircleDownIcon,
  ExclamationCircleIcon,
  XCircleIcon,
  MenuIcon,
  RefreshIcon,
} from "@heroicons/vue/outline";
import animations from "../animations.js";
import SpinnerIcon from "./SpinnerIcon.vue";

// Socket message variables and handlers
const crawlerStatus = ref("Ready");
const autoCrawlerEnabled = ref(false);
const autoCrawlerTriggerDate = ref("01/01/2022");
watch(crawlerStatus, () => {
  console.log("crawler status triggered, send ws message");
});
watch(autoCrawlerEnabled, () => {
  console.log("crawler enabled triggered, send ws message");
});
watch(autoCrawlerTriggerDate, () => {
  console.log("crawler date triggered, send ws message");
});
// ** Update to use websocket values after testing

// Template refs
const refForceButtonText = ref();
const refForceButtonIcon = ref();
watch(
  () => refForceButtonText.value?.className,
  (newVal, oldVal) => {
    console.log(refForceButtonText.value?.className);
    if (typeof oldVal === "undefined") {
      return;
    }

    animations[crawlButtonState.value.animation + "Enter"](
      refForceButtonText.value
    );
  }
);
let ani;
watch(
  () => refForceButtonIcon.value,
  (newVal, oldVal) => {
    if (typeof oldVal === "undefined") {
      return;
    }
    ani = animations.Spin(refForceButtonIcon.value);

    if (crawlButtonState.value.animation == "ButtonDrain") {
      ani.play();
    } else {
      ani.pause();
    }
  }
);
// watchEffect(() => {
//   console.log(refForceButtonText.value);
//   console.log(refForceButtonIcon.value);
// });

// States
const crawlButtonState = ref({ isActive: true, animation: null });
const panelState = ref({
  label: "Enter date",
  animation: null,
  editDate: null,
});
const statusState = ref({
  currentStatus: null,
  icons: [
    StatusOnlineIcon,
    ArrowCircleDownIcon,
    ExclamationCircleIcon,
    XCircleIcon,
  ],
  setStatus: () => {
    if (crawlerStatus.value == "Ready") {
      statusState.value.currentStatus = statusState.value.icons[0];
    } else if (crawlerStatus.value == "In Progress") {
      statusState.value.currentStatus = statusState.value.icons[1];
    } else if (crawlerStatus.value == "Error") {
      statusState.value.currentStatus = statusState.value.icons[2];
    } else if (crawlerStatus.value == "Disabled") {
      statusState.value.currentStatus = statusState.value.icons[3];
    }
  },
});
statusState.value.setStatus();

// Watchers
// To validate date
watch(
  () => panelState.value.editDate,
  () => {
    const dateString = panelState.value.editDate.split("-");
    const newDate = new Date(dateString[0], dateString[1] - 1, dateString[2]);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    if (newDate >= today) {
      autoCrawlerTriggerDate.value =
        dateString[1] + "/" + dateString[2] + "/" + dateString[0];

      panelState.value.animation = "Flash";
      panelState.value.label = "Date updated";
    } else {
      panelState.value.animation = "HeadShake";
      panelState.value.label = "Invalid date";
    }
  }
);
// To switch out Status icon
watch(
  () => crawlerStatus.value,
  () => statusState.value.setStatus()
);

// Events
function PanelButtonClicked(isPanelOpen, ClosePanel, event) {
  if (crawlButtonState.value.isActive == false) {
    ClosePanel();
  }
  panelState.value.label = "Enter date";

  const el = event.target.childNodes[0];

  if (isPanelOpen == false) {
    anime({
      targets: el,
      rotate: {
        value: "+=0.25turn",
        duration: 500,
        easing: "easeInOutSine",
      },
    });
  } else {
    anime({
      targets: el,
      rotate: {
        value: "-=0.25turn",
        duration: 500,
        easing: "easeInOutSine",
      },
    });
  }
}
function CrawlButtonClicked(ClosePanel, event) {
  if (crawlButtonState.value.isActive) {
    crawlButtonState.value.isActive = false;
    crawlButtonState.value.animation = "ButtonDrain";
    ClosePanel();

    // TODO: remove
    crawlerStatus.value = "In Progress";
  } else {
    crawlButtonState.value.isActive = true;
    crawlButtonState.value.animation = "ButtonFill";

    // TODO: remove
    crawlerStatus.value = "Ready";
  }
}
</script>

<template>
  <!-- TODO: remove -->
  <div class="my-10"></div>

  <div
    class="mx-4 select-none max-w-sm bg-white rounded-lg shadow divide-y divide-gray-200"
  >
    <div class="flex items-center justify-between p-6">
      <div>
        <div class="flex items-center">
          <p class="text-gray-900 text-sm font-medium">SmartMatch</p>
          <!-- <AnimationHandler animation="FadeIn"> -->
          <div
            :key="crawlerStatus"
            :class="{
              'text-green-800 bg-green-100': crawlerStatus == 'Ready',
              'text-yellow-800 bg-yellow-100': crawlerStatus == 'In Progress',
              'text-red-800 bg-red-100': crawlerStatus == 'Error',
              'text-gray-800 bg-gray-100': crawlerStatus == 'Disabled',
              'ml-3 px-2 py-0.5 text-xs font-medium rounded-full': true,
            }"
          >
            {{ crawlerStatus }}
          </div>
          <!-- </AnimationHandler> -->
          <!-- <AnimationHandler animation="FadeIn"> -->
          <component
            :is="statusState.currentStatus"
            :class="{
              'text-green-500':
                statusState.currentStatus == statusState.icons[0],
              'text-yellow-500':
                statusState.currentStatus == statusState.icons[1],
              'text-red-500': statusState.currentStatus == statusState.icons[2],
              'text-gray-500':
                statusState.currentStatus == statusState.icons[3],
              'h-5 w-5 ml-1': true,
            }"
          ></component>
          <!-- </AnimationHandler> -->
        </div>
        <div class="flex items-center mt-2 text-gray-500 text-sm">
          <p>AutoCrawl:</p>
          <input
            :key="crawlButtonState.isActive"
            type="checkbox"
            v-model="autoCrawlerEnabled"
            class="flex items-center ml-2 cursor-pointer focus:ring-indigo-500 h-4 w-4 text-indigo-600 border-gray-300 rounded"
          />
        </div>
        <div class="text-gray-500 text-sm">
          <span>Next AutoCrawl: </span>
          <AnimationHandler animation="FadeIn">
            <span :key="autoCrawlerTriggerDate">{{
              autoCrawlerTriggerDate
            }}</span>
          </AnimationHandler>
        </div>
      </div>
      <img
        class="w-20 h-20 border rounded-full"
        src="../assets/SmartMatchLogo.png"
      />
    </div>
    <Disclosure
      as="div"
      v-slot="{ open, close }"
      class="flex justify-between divide-x divide-gray-200"
    >
      <div class="flex flex-1 justify-center">
        <div>
          <AnimationHandler
            :animation="crawlButtonState.animation"
            args="panelbutton"
          >
            <DisclosureButton
              :key="crawlButtonState.isActive"
              @click="PanelButtonClicked(open, close, $event)"
              :class="{
                'bg-indigo-100 text-indigo-700':
                  crawlButtonState.isActive == true,
                'bg-gray-500 text-white bg-[length:0%,0%] cursor-not-allowed':
                  crawlButtonState.isActive == false,
                'bg-[length:0%,0%] bg-indigo-200 hover:bg-indigo-100':
                  crawlButtonState.isActive == true && open == true,
                'bg-[length:0%,0%] bg-indigo-100 hover:bg-indigo-200':
                  crawlButtonState.isActive == true && open == false,
                'flex items-center mx-6 my-4 px-2 py-2 max-h-8 bg-gradient-to-r from-indigo-100 to-indigo-100 bg-no-repeat bg-center border border-transparent text-sm leading-4 font-medium rounded-md focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500': true,
              }"
              ><MenuIcon class="h-5 w-5 mr-1" />Edit AutoCrawl
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
                  v-model="panelState.editDate"
                  class="mx-1 shadow-sm focus:ring-indigo-500 focus:border-indigo-500 border-gray-300 rounded-md"
                  placeholder="MM/DD/YYYY"
                />
              </div>
              <AnimationHandler :animation="panelState.animation">
                <p
                  :key="panelState.editDate"
                  class="mx-1 text-gray-500 mt-2 mb-4 t)ext-sm"
                >
                  {{ panelState.label }}
                </p>
              </AnimationHandler>
            </DisclosurePanel>
          </AnimationHandler>
        </div>
      </div>
      <div class="flex flex-1 justify-center items-center">
        <div>test</div>
        <!-- <AnimationHandler :animation="crawlButtonState.animation"> -->
        <button
          ref="refForceButtonText"
          id="ForceButton"
          :key="crawlButtonState.isActive"
          type="button"
          @click="CrawlButtonClicked(close, $event)"
          :class="{
            'bg-indigo-600 bg-[length:150%,150%] hover:bg-[length:0%,0%] hover:bg-indigo-700':
              crawlButtonState.isActive == true,
            'bg-gray-500 bg-[length:0%,0%] cursor-not-allowed':
              crawlButtonState.isActive == false,
            'flex items-center my-4 px-2 py-2 max-h-8 bg-gradient-to-r from-indigo-600 to-indigo-600 bg-no-repeat bg-center border border-transparent text-sm leading-4 font-medium rounded-md shadow-sm text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500': true,
          }"
        >
          <!-- :key="crawlButtonState.isActive" -->
          Force Crawl
        </button>
        <!-- </AnimationHandler> -->
      </div>
    </Disclosure>
  </div>
  <!-- <CrawlerList></CrawlerList>
  <MenuIcon class="h-6 w-6"></MenuIcon> -->
  <Teleport to="#ForceButton">
    <RefreshIcon ref="refForceButtonIcon" class="h-5 w-5 mr-1" />
  </Teleport>
</template>
