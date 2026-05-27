import ws from 'k6/ws'

export const options = {
  vus: 1000,
  duration: '60s',
}

export default function () {
  ws.connect('ws://127.0.0.1:8080/ws', {}, function (socket) {

    socket.on('open', () => {
      socket.send('ping')
    })

    socket.on('message', () => {})

    socket.setTimeout(() => {
      socket.close()
    }, 1000)
  })
}
