<template>
    <ul class="border">
        <li v-for="event in timeline" :key="event.id">
            <div class="pb-2">
                <div class="flex">
                    <div>
                        <span :class="[event.iconBackground, 'h-8 w-8 rounded-full flex items-center justify-center ring-8 ring-white']">
                            <component :is="event.icon" class="h-5 w-5 text-white"/>
                        </span>
                    </div>
                    <div class="min-w-0 flex-1 pt-1.5 flex justify-between">
                        <div>
                            <p class="text-sm text-gray-500 ml-1">
                                {{ event.content }}
                                <span class="font-medium text-gray-900">{{ event.target }}</span>
                            </p>
                        </div>
                        <div class="text-right text-sm whitespace-nowrap text-gray-500">
                            <time :datetime="event.datetime">{{ event.date }}</time>
                        </div>
                    </div>
                </div>
            </div>
        </li>
    </ul>
</template>

<script>
import { CheckIcon, ThumbUpIcon } from '@heroicons/vue/solid'

const timeline = [
  {
    id: 1,
    content: 'Completed',
    target: 'SmartMatch Feb22',
    href: '#',
    date: 'Sep 20',
    datetime: '2020-09-20',
    icon: ThumbUpIcon,
    iconBackground: 'bg-blue-500',
  },
  {
    id: 2,
    content: 'Completed',
    target: 'Parascript Feb22',
    href: '#',
    date: 'Sep 22',
    datetime: '2020-09-22',
    icon: CheckIcon,
    iconBackground: 'bg-blue-500',
  },
  {
    id: 3,
    content: 'Completed',
    target: 'RoyalMail Feb22',
    href: '#',
    date: 'Sep 28',
    datetime: '2020-09-28',
    icon: ThumbUpIcon,
    iconBackground: 'bg-blue-500',
  },  
  {
    id: 4,
    content: 'Asdf',
    target: 'YesNo Feb22',
    href: '#',
    date: 'Sep 28',
    datetime: '2020-09-28',
    icon: ThumbUpIcon,
    iconBackground: 'bg-blue-500',
  },  
  {
    id: 5,
    content: 'Completed',
    target: 'RoyalMail Feb22',
    href: '#',
    date: 'Sep 28',
    datetime: '2020-09-28',
    icon: ThumbUpIcon,
    iconBackground: 'bg-blue-500',
  },  
]

export default {
    data() {
        return {
            timeline
        }
    }
}
</script>
