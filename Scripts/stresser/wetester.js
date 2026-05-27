import ws from 'k6/ws'

export const options = {
  vus: 200,
  duration: '30s',
}

export default function () {
  ws.connect('ws://127.0.0.1:8080/ws/live', {}, function (socket) {
    socket.on('message', () => {})
  })
}
