<template>
  <div class="flex-grow mx-5 truncate bg-white rounded-lg shadow">
        <div class="flex items-center justify-between p-6 flex-wrap sm:flex-nowrap">
            <div class="mr-14">
                <p class="text-gray-900 text-sm font-medium">Service Status</p>
                <div class="flex items-center mt-2 text-gray-500 text-sm">
                  <p class="mr-2">Service Active:</p> 
                  <CheckCircleIcon v-if="builderStatus.active" class="h-7 w-7"/>
                  <XCircleIcon v-else class="h-7 w-7"/>
                </div>
                <div v-if="builderStatus.active">
                  <p class="mt-2 text-gray-500 text-sm">Last Check-in: {{builderStatus.timestamp.checkinDate}}</p>
                  <p class="text-gray-500 text-sm">@ {{builderStatus.timestamp.checkinTime}}</p>
                </div>
            </div>
        
            <LogFeed class="flex-grow max-w-md max-h-36 overflow-auto mt-2 sm:mt-0"/>
        </div>
  </div>
</template>

<script>
import LogFeed from "./LogFeed.vue"
import { CheckCircleIcon, XCircleIcon } from '@heroicons/vue/outline'

export default {
  name: "StatusCard",
  props: {
    builderStatus: Object
  },
  components: { 
    LogFeed,
    CheckCircleIcon,
    XCircleIcon,
  },
}
</script>
