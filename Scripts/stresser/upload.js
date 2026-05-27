import http from 'k6/http'

const binFile = open('./big.bin', 'b')

export const options = {
  vus: 100,
  duration: '60s',
}

export default function () {
  const data = {
    file: http.file(binFile, 'big.bin'),
  }

  http.post(
    'http://127.0.0.1:8080/upload',
    data
  )
}
