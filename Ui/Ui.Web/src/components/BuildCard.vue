<template>
    <div class="flex-grow max-w-md truncate bg-white rounded-lg shadow divide-y divide-gray-200">
        <div class="flex items-center justify-between p-6">
            <div class="mr-4">
                <div class="flex">
                    <p class="text-gray-900 text-sm font-medium">{{ props.dirType }}</p>
                    <p :class="statusBadgeStyle">{{ statusBadge }}</p>
                </div>
                
                <p class="mt-2 text-gray-500 text-sm">Select directory month:</p>
                
                <p v-if="isbuildStarted" class="mt-4">Building: {{currentBuild}}</p>
                <select v-else name="directoryMonth" id="dirmonth" v-model="selectedBuild" class="border mt-2 block w-full pl-3 pr-10 py-2 text-base border-gray-300 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm rounded-md">
                    <option v-for="(dir, index) in availableBuilds" :key="index">{{ dir[1] }}</option>
                </select>
            </div>
        
            <img class="w-20 h-20 bg-gray-300 border rounded-full" :src="avatar"/>
        </div>
        
        <div class="flex justify-between">           
            <div class="flex flex-grow mx-3 my-8 p-0.5 bg-gray-200 rounded-full">
                <p class="bg-indigo-500 p-0.5 text-xs font-medium text-white text-center leading-none rounded-full" :style="{width: `${props.builder.progress}%`}">{{ props.builder.progress }}%</p>
            </div>
        
            <button @click="BuildDirectory()" :class="buttonStyle">
                <MailIcon class="mr-2 h-4 w-4 text-white"></MailIcon>
                <p class="text-sm leading-4 font-medium text-white">Build</p>
            </button>
        </div>
    </div>
</template>

<script setup>
import { defineProps, ref, toRef, reactive, watch } from 'vue'
import { MailIcon } from '@heroicons/vue/solid'
import SelectMenu from './SelectMenu.vue'
import axios from 'axios'
import classes from './Classes'

const props = defineProps({
    dirType: String,
    crawler: Object,
    builder: Object,
})

// Template variables
let avatar = ref("")
if (props.dirType === "SmartMatch") {
    avatar.value = "http://ewr1.vultrobjects.com/raf-website/SmartMatchLogo.png"
}
if (props.dirType === "Parascript") {
    avatar.value = "http://ewr1.vultrobjects.com/raf-website/ParascriptLogo.png"
}
if (props.dirType === "RoyalMail") {
    avatar.value = "https://ewr1.vultrobjects.com/raf-website/RoyalMailLogo.png"
}

// Style template variables
let statusBadge = ref("")
let statusBadgeStyle = ref("")
let buttonStyle = ref("")

// Build variables
let isBuildStarted = ref(false)
let buildMonth = 0
let buildYear = 0
let currentBuild = ref("")
let availableBuilds = ref([])
let selectedBuild = ref("")

watch(props.builder, () => {
    // Check status, set statusBadge and statusBadgeStyle
    if (props.builder.status === 0) {
        // Quick fix for bug where watched prop updates before the next status Get after BuildDirectory() is called
        if (isBuildStarted.value === true) {
            setTimeout(isBuildStarted.value = false, 3000)
            return
        }

        SetStyle("Ready")
    }
    if (props.builder.status === 1) {
        SetStyle("In Progress")
        currentBuild.value = props.builder.currentBuild
    }
    if (props.builder.status === 2) {
        SetStyle("Error")
    }

    // Populate available builds, format for select menu
    props.crawler.availableBuilds.forEach(build => {
        availableBuilds.value.length = 0

        buildYear = parseInt(build.substring(0, 4))
        buildMonth = parseInt(build.substring(4, 6) - 1)
        const buildDate = new Date(buildYear, buildMonth)

        const months = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"]
        const buildMonthLong = months[buildDate.getMonth()]

        const buildGroup = [build, buildMonthLong + " " + buildYear]

        availableBuilds.value.push(buildGroup)
    });
})

function SetStyle(status) {
    if (status === "Ready") {
        statusBadge.value = "Ready"
        statusBadgeStyle.value = "ml-3 px-2 py-0.5 text-green-800 text-xs font-medium bg-green-100 rounded-full"
        buttonStyle.value = "flex items-center px-3 mr-6 my-4 rounded-md bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
    }
    if (status === "In Progress") {
        statusBadge.value = "In Progress"
        statusBadgeStyle.value = "ml-3 px-2 py-0.5 text-yellow-800 text-xs font-medium bg-yellow-100 rounded-full"
        buttonStyle.value = "flex items-center px-3 mr-6 my-4 rounded-md bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 cursor-not-allowed opacity-50"
    }
    if (status === "Error") {
        statusBadge.value = "Error"
        statusBadgeStyle.value = "ml-3 px-2 py-0.5 text-red-800 text-xs font-medium bg-red-100 rounded-full"
        buttonStyle.value = "flex items-center px-3 mr-6 my-4 rounded-md bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 cursor-not-allowed opacity-50"            
    }
}

function BuildDirectory() {
    if (isBuildStarted.value === true) {
        return
    }
    if (selectedBuild.value === "") {
        return
    }

    isBuildStarted.value = true

    // Style badge, button
    SetStyle("In Progress")

    // Create body for Post request
    let smartMatchActive = false
    let parascriptActive = false
    let royalmailActive = false
    if (props.dirType === "SmartMatch") {
        smartMatchActive = true
    }
    if (props.dirType === "Parascript") {
        parascriptActive = true
    }
    if (props.dirType === "RoyalMail") {
        royalmailActive = true
    }

    let postMonth = ""
    let postYear = ""

    for (let index = 0; index < availableBuilds.value.length; index++) {
        if (availableBuilds.value[index][1] === selectedBuild.value) {
            postYear = availableBuilds.value[index][0].substring(0, 4)

            if (parseInt(availableBuilds.value[index][0].substring(4, 6)) < 10) {
                postMonth = availableBuilds.value[index][0].substring(5, 6)                
            }
            else {
                postMonth = availableBuilds.value[index][0].substring(4, 6)
            }
        }
    }

    const body = {
	"BuildSmartMatch": smartMatchActive,
	"BuildParascript": parascriptActive,
	"BuildRoyalMail": royalmailActive,
	
	"Month": postMonth,
	"Year": postYear,
	"SmUser": "",
	"SmPass": "",
	"Key": "",
	
	"CheckStatus": false
    }

    console.log(body)

    axios.post(`https://localhost:5001/api/Builder`, body)
    .then(response => {
        console.log(response.data)
    })
    .catch(() => {
        console.log("error")
    })
}
</script>